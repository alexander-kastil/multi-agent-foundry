using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;

static class Foundry
{
    public const string AgentName = "m7c2-ticket-triage-agent";
    public const string ToolAgentName = "m7c2-tool-agent";
    public const string EvaluatorName = "m7c2_tool_choice";
    public const string EvaluationPrefix = "m7c2-triage-";
    public const string BaselineInstructions = "You triage customer support tickets for a chocolate shop.";

    public static readonly string Endpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!.TrimEnd('/');
    public static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
    public static readonly string EvalModel = Environment.GetEnvironmentVariable("EVAL_MODEL_DEPLOYMENT_NAME")!;
    public static readonly string OptimizationModel = Environment.GetEnvironmentVariable("OPTIMIZATION_MODEL_DEPLOYMENT_NAME")!;

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };
    static readonly DefaultAzureCredential Credential = new();

    public static async Task<JsonNode?> Project(HttpMethod method, string path, object? body = null)
    {
        string separator = path.Contains('?') ? "&" : "?";
        return await Send(method, $"{Endpoint}{path}{separator}api-version=v1", body);
    }

    public static async Task<JsonNode?> OpenAI(HttpMethod method, string path, object? body = null) =>
        await Send(method, $"{Endpoint}/openai/v1{path}", body);

    static async Task<JsonNode?> Send(HttpMethod method, string url, object? body)
    {
        AccessToken token = await Credential.GetTokenAsync(new TokenRequestContext(["https://ai.azure.com/.default"]));
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        request.Headers.Add("Foundry-Features", "Evaluations=V1Preview");
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }
        using HttpResponseMessage response = await Http.SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"{(int)response.StatusCode} {method} {url}\n{text}");
        }
        return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
    }

    public static List<(string Query, string GroundTruth)> LoadTickets() =>
        File.ReadAllLines("tickets.jsonl")
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonNode.Parse(line)!)
            .Select(row => (row["query"]!.GetValue<string>(), row["ground_truth"]!.GetValue<string>()))
            .ToList();

    public static async Task<JsonNode> CreateAgentVersion(string instructions) =>
        (await Project(HttpMethod.Post, $"/agents/{AgentName}/versions", new
        {
            definition = new { kind = "prompt", model = Model, instructions },
        }))!;

    public static async Task<string> LatestVersion(string agentName)
    {
        JsonNode agent = (await Project(HttpMethod.Get, $"/agents/{agentName}"))!;
        return agent["versions"]!["latest"]!["version"]!.GetValue<string>();
    }

    public static async Task<List<JsonNode>> List(Func<string, Task<JsonNode?>> get)
    {
        var items = new List<JsonNode>();
        string? after = null;
        while (true)
        {
            JsonNode page = (await get(after is null ? "limit=100" : $"limit=100&after={after}"))!;
            JsonArray data = page["data"]!.AsArray();
            items.AddRange(data.Select(item => item!));
            if (!(page["has_more"]?.GetValue<bool>() ?? false) || data.Count == 0)
            {
                return items;
            }
            after = data[^1]!["id"]!.GetValue<string>();
        }
    }

    public static async Task<(double Score, int Count)> ScoreVersion(string version)
    {
        JsonNode evaluation = (await OpenAI(HttpMethod.Post, "/evals", new
        {
            name = $"{EvaluationPrefix}v{version}",
            data_source_config = new
            {
                type = "custom",
                item_schema = new
                {
                    type = "object",
                    properties = new { query = new { type = "string" }, ground_truth = new { type = "string" } },
                    required = new[] { "query", "ground_truth" },
                },
                include_sample_schema = true,
            },
            testing_criteria = new[]
            {
                new
                {
                    type = "azure_ai_evaluator",
                    name = "f1",
                    evaluator_name = "builtin.f1_score",
                    data_mapping = new { response = "{{sample.output_text}}", ground_truth = "{{item.ground_truth}}" },
                },
            },
        }))!;
        string evalId = evaluation["id"]!.GetValue<string>();
        JsonNode run = (await OpenAI(HttpMethod.Post, $"/evals/{evalId}/runs", new
        {
            name = $"{EvaluationPrefix}v{version}-run",
            data_source = new
            {
                type = "azure_ai_target_completions",
                source = new
                {
                    type = "file_content",
                    content = LoadTickets().Select(ticket => new { item = new { query = ticket.Query, ground_truth = ticket.GroundTruth } }),
                },
                input_messages = new
                {
                    type = "template",
                    template = new[]
                    {
                        new { type = "message", role = "user", content = new { type = "input_text", text = "{{item.query}}" } },
                    },
                },
                target = new { type = "azure_ai_agent", name = AgentName, version },
            },
        }))!;
        string runId = run["id"]!.GetValue<string>();
        while (run["status"]!.GetValue<string>() is not ("completed" or "failed" or "canceled"))
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            run = (await OpenAI(HttpMethod.Get, $"/evals/{evalId}/runs/{runId}"))!;
        }
        List<JsonNode> items = await List(query => OpenAI(HttpMethod.Get, $"/evals/{evalId}/runs/{runId}/output_items?{query}"));
        List<double> scores = items.SelectMany(item => item["results"]!.AsArray()).Select(result => result!["score"]!.GetValue<double>()).ToList();
        return (scores.Average(), scores.Count);
    }

    public static async Task<JsonNode> CreateOptimizationJob(string displayName, string agentName, string version, string attribute, int maxCandidates, object evaluator, IEnumerable<object> testCases, int? maxConcurrentRuns) =>
        (await Project(HttpMethod.Post, "/agent_optimization_jobs", new
        {
            display_name = displayName,
            target_configuration = new { name = agentName, version, type = "foundry_agent" },
            optimization_model_configuration = new { model = OptimizationModel },
            optimization_configuration = new
            {
                evaluation_configuration = new
                {
                    training_set = new { source = new { test_cases = testCases, type = "inline" }, type = "target_completion" },
                    evaluators = new[] { evaluator },
                    evaluation_model = new { model = EvalModel },
                    max_concurrent_agent_runs = maxConcurrentRuns,
                },
                candidate_search_configuration = new { max_candidates = maxCandidates },
                agent_optimization_space = new { target_attributes = new[] { attribute } },
                type = "agent_optimization",
            },
        }))!;

    public static async Task<JsonNode> WaitForJob(string jobId)
    {
        while (true)
        {
            JsonNode job = (await Project(HttpMethod.Get, $"/agent_optimization_jobs/{jobId}"))!;
            string status = job["status"]!.GetValue<string>();
            Console.WriteLine($"  status {status}");
            if (status is "succeeded" or "failed" or "cancelled")
            {
                return job;
            }
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
    }

    public static async Task<List<JsonNode>> Candidates(string jobId, bool mutations = false) =>
        await List(query => Project(HttpMethod.Get, $"/agent_optimization_jobs/{jobId}/candidates?{query}{(mutations ? "&expand=mutations" : "")}"));
}
