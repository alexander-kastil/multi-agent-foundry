using System.Text.Json;
using System.Text.Json.Nodes;

switch (args.FirstOrDefault())
{
    case "create-agent": await CreateAgent(); break;
    case "continuous-rule": await ContinuousRule(); break;
    case "evaluate-responses": await EvaluateResponses(); break;
    case "evaluate-traces": await EvaluateTraces(); break;
    case "red-team": await RedTeam(); break;
    case "red-team-report": RedTeamReport(); break;
    case "cleanup": await Cleanup(); break;
    default: Console.WriteLine("create-agent | continuous-rule | evaluate-responses | evaluate-traces | red-team | red-team-report | cleanup"); break;
}

static object[] ResponseCriteria() =>
[
    new { type = "azure_ai_evaluator", name = "violence", evaluator_name = "builtin.violence" },
    new
    {
        type = "azure_ai_evaluator",
        name = "coherence",
        evaluator_name = "builtin.coherence",
        initialization_parameters = new { deployment_name = Foundry.Model },
    },
];

static async Task<JsonNode> CreateEvaluation(string name, string scenario, object[] criteria) =>
    (await Foundry.OpenAI(HttpMethod.Post, "/evals", new
    {
        name,
        data_source_config = new { type = "azure_ai_source", scenario },
        testing_criteria = criteria,
    }))!;

static async Task<List<JsonNode>> OutputItems(string evalId, string runId) =>
    await Foundry.List(query => Foundry.OpenAI(HttpMethod.Get, $"/evals/{evalId}/runs/{runId}/output_items?{query}"));

static async Task CreateAgent()
{
    JsonNode agent = await Foundry.CreateAgentVersion();
    string version = agent["version"]!.GetValue<string>();
    Console.WriteLine($"agent {Foundry.AgentName} version {version}");
    var ids = new List<string>();
    foreach (string question in Foundry.Questions)
    {
        JsonNode response = await Foundry.AskAgent(version, question);
        string id = response["id"]!.GetValue<string>();
        ids.Add(id);
        Console.WriteLine($"{Foundry.Cut(id, 18)}  '{Foundry.Cut(Foundry.OutputText(response), 90)}'");
    }
    File.WriteAllText("response_ids.json", JsonSerializer.Serialize(ids));
}

static async Task ContinuousRule()
{
    JsonNode agent = (await Foundry.Project(HttpMethod.Get, $"/agents/{Foundry.AgentName}"))!;
    string version = agent["versions"]!["latest"]!["version"]!.GetValue<string>();

    JsonNode evaluation = await CreateEvaluation("m7c2-continuous-evaluation", "responses", ResponseCriteria());
    string evalId = evaluation["id"]!.GetValue<string>();
    Console.WriteLine($"evaluation {evalId}");

    JsonNode rule = (await Foundry.Project(HttpMethod.Put, $"/evaluationrules/{Foundry.RuleId}", new
    {
        displayName = "m7c2 continuous evaluation",
        description = "Evaluate every completed response of the support agent",
        action = new { type = "continuousEvaluation", evalId, maxHourlyRuns = 100 },
        eventType = "responseCompleted",
        filter = new { agentName = Foundry.AgentName },
        enabled = true,
    }))!;
    Console.WriteLine($"rule {rule["id"]} enabled={rule["enabled"]} event={rule["eventType"]}");

    foreach (string question in Foundry.Questions)
    {
        await Foundry.AskAgent(version, question);
    }
    Console.WriteLine($"sent {Foundry.Questions.Length} questions to {Foundry.AgentName} version {version}");

    List<JsonNode> runs = [];
    for (int attempt = 0; attempt < 30; attempt++)
    {
        runs = await Foundry.List(query => Foundry.OpenAI(HttpMethod.Get, $"/evals/{evalId}/runs?{query}"));
        if (runs.Count > 0 && runs.All(run => run["status"]!.GetValue<string>() is "completed" or "failed" or "canceled"))
        {
            break;
        }
        await Task.Delay(TimeSpan.FromSeconds(20));
    }
    foreach (JsonNode run in runs)
    {
        Console.WriteLine($"run {Foundry.Cut(run["id"]!.GetValue<string>(), 24)}  {run["status"]}  {Foundry.Counts(run)}");
    }
}

static async Task EvaluateResponses()
{
    string[] ids = JsonSerializer.Deserialize<string[]>(File.ReadAllText("response_ids.json"))!;
    JsonNode evaluation = await CreateEvaluation("m7c2-response-evaluation", "responses", ResponseCriteria());
    string evalId = evaluation["id"]!.GetValue<string>();
    JsonNode run = (await Foundry.OpenAI(HttpMethod.Post, $"/evals/{evalId}/runs", new
    {
        name = "m7c2-stored-responses",
        data_source = new
        {
            type = "azure_ai_responses",
            item_generation_params = new
            {
                type = "response_retrieval",
                data_mapping = new { response_id = "{{item.resp_id}}" },
                source = new { type = "file_content", content = ids.Select(id => new { item = new { resp_id = id } }) },
            },
        },
    }))!;
    run = await Foundry.WaitForRun(evalId, run["id"]!.GetValue<string>(), 10);
    Console.WriteLine($"run {run["status"]}  {Foundry.Counts(run)}");
    foreach (JsonNode item in await OutputItems(evalId, run["id"]!.GetValue<string>()))
    {
        string verdicts = string.Join(" ", item["results"]!.AsArray().Select(result => $"{result!["name"]}={result["score"]!.GetValue<double>():0.0}"));
        Console.WriteLine($"{Foundry.Cut(item["datasource_item"]!["response_id"]!.GetValue<string>(), 18)}  {verdicts}");
    }
}

static async Task EvaluateTraces()
{
    JsonNode agent = (await Foundry.Project(HttpMethod.Get, $"/agents/{Foundry.AgentName}"))!;
    string agentId = $"{Foundry.AgentName}:{agent["versions"]!["latest"]!["version"]}";
    object Criterion(string name) => new
    {
        type = "azure_ai_evaluator",
        name,
        evaluator_name = $"builtin.{name}",
        initialization_parameters = new { deployment_name = Foundry.Model },
        data_mapping = new { query = "{{item.query}}", response = "{{item.response}}" },
    };
    JsonNode evaluation = await CreateEvaluation("m7c2-trace-evaluation", "traces", [Criterion("intent_resolution"), Criterion("task_adherence")]);
    string evalId = evaluation["id"]!.GetValue<string>();
    JsonNode run = (await Foundry.OpenAI(HttpMethod.Post, $"/evals/{evalId}/runs", new
    {
        name = "m7c2-agent-traces",
        data_source = new { type = "azure_ai_traces", agent_id = agentId, max_traces = 20, lookback_hours = 2 },
    }))!;
    run = await Foundry.WaitForRun(evalId, run["id"]!.GetValue<string>(), 10);
    Console.WriteLine($"agent {agentId}");
    Console.WriteLine($"run {run["status"]}  {Foundry.Counts(run)}");
    foreach (JsonNode item in await OutputItems(evalId, run["id"]!.GetValue<string>()))
    {
        string verdicts = string.Join(" ", item["results"]!.AsArray().Select(result => $"{result!["name"]}={(result["passed"]!.GetValue<bool>() ? "pass" : "fail")}"));
        JsonNode source = item["datasource_item"]!;
        string response = $"'{Foundry.Cut(source["response"]!.GetValue<string>(), 46)}'";
        Console.WriteLine($"{Foundry.Cut(source["trace_id"]!.GetValue<string>(), 8)}  {response,-50}  {verdicts}");
    }
}

static async Task RedTeam()
{
    JsonNode agent = await Foundry.CreateAgentVersion();
    string version = agent["version"]!.GetValue<string>();
    var target = new { name = Foundry.AgentName, version, type = "azure_ai_agent" };

    object Safety(string name, string evaluator) => new { type = "azure_ai_evaluator", name, evaluator_name = evaluator, evaluator_version = "1" };
    JsonNode evaluation = await CreateEvaluation("m7c2-red-team-evaluation", "red_team",
    [
        Safety("Prohibited Actions", "builtin.prohibited_actions"),
        new
        {
            type = "azure_ai_evaluator",
            name = "Task Adherence",
            evaluator_name = "builtin.task_adherence",
            evaluator_version = "1",
            initialization_parameters = new { deployment_name = Foundry.Model },
        },
        Safety("Sensitive Data Leakage", "builtin.sensitive_data_leakage"),
    ]);
    string evalId = evaluation["id"]!.GetValue<string>();

    JsonNode taxonomy = (await Foundry.Project(HttpMethod.Put, $"/evaluationtaxonomies/{Foundry.AgentName}", new
    {
        description = "Taxonomy for red teaming the support agent",
        taxonomyInput = new { type = "agent", riskCategories = new[] { "ProhibitedActions" }, target },
    }))!;

    JsonNode run = (await Foundry.OpenAI(HttpMethod.Post, $"/evals/{evalId}/runs", new
    {
        name = "m7c2-red-team-run",
        data_source = new
        {
            type = "azure_ai_red_team",
            item_generation_params = new
            {
                type = "red_team_taxonomy",
                attack_strategies = new[] { "Flip", "Base64" },
                num_turns = 3,
                source = new { type = "file_id", id = taxonomy["id"]!.GetValue<string>() },
            },
            target,
        },
    }))!;
    string runId = run["id"]!.GetValue<string>();
    Console.WriteLine($"red team run {runId} started against {Foundry.AgentName} version {version}");
    DateTime started = DateTime.UtcNow;
    run = await Foundry.WaitForRun(evalId, runId, 15, current => Console.WriteLine($"  {(int)(DateTime.UtcNow - started).TotalSeconds}s {current["status"]}"));
    Console.WriteLine($"run {run["status"]}  {Foundry.Counts(run)}");
    List<JsonNode> items = await OutputItems(evalId, runId);
    File.WriteAllText("red_team_items.json", JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"{items.Count} output items written to red_team_items.json");
}

static void RedTeamReport()
{
    JsonArray items = JsonNode.Parse(File.ReadAllText("red_team_items.json"))!.AsArray();
    var totals = new Dictionary<string, (int Pass, int Fail)>();
    var byTechnique = new Dictionary<string, (int Flagged, int Clear)>();
    var flagged = new List<(string Technique, string Reason, string Reply)>();
    foreach (JsonNode? item in items)
    {
        foreach (JsonNode? result in item!["results"]!.AsArray())
        {
            string name = result!["name"]!.GetValue<string>();
            bool passed = result["passed"]!.GetValue<bool>();
            (int pass, int fail) = totals.GetValueOrDefault(name);
            totals[name] = passed ? (pass + 1, fail) : (pass, fail + 1);
            if (name == "prohibited_actions")
            {
                string technique = result["properties"]!["attack_technique"]!.GetValue<string>();
                (int flaggedCount, int clear) = byTechnique.GetValueOrDefault(technique);
                byTechnique[technique] = passed ? (flaggedCount, clear + 1) : (flaggedCount + 1, clear);
                if (!passed)
                {
                    JsonArray output = item["sample"]!["output"]!.AsArray();
                    flagged.Add((technique, result["reason"]!.GetValue<string>(), output[^1]!["content"]!.GetValue<string>()));
                }
            }
        }
    }
    Console.WriteLine($"{items.Count} output items");
    foreach ((string name, (int pass, int fail)) in totals)
    {
        Console.WriteLine($"{name,-20} pass {pass,3}  fail {fail,3}");
    }
    foreach ((string technique, (int flaggedCount, int clear)) in byTechnique)
    {
        Console.WriteLine($"  {technique,-10} flagged {flaggedCount,2} of {flaggedCount + clear}");
    }
    Console.WriteLine();
    foreach ((string technique, string reason, string reply) in flagged.Take(3))
    {
        Console.WriteLine($"[{technique}] judge: {Foundry.Cut(reason, 130)}");
        Console.WriteLine($"           agent: '{Foundry.Cut(reply, 130)}'");
    }
}

static async Task Cleanup()
{
    await Foundry.Project(HttpMethod.Delete, $"/evaluationrules/{Foundry.RuleId}");
    Console.WriteLine($"deleted rule {Foundry.RuleId}");
    foreach (JsonNode evaluation in await Foundry.List(query => Foundry.OpenAI(HttpMethod.Get, $"/evals?{query}")))
    {
        string name = evaluation["name"]!.GetValue<string>();
        if (Foundry.EvaluationNames.Contains(name))
        {
            await Foundry.OpenAI(HttpMethod.Delete, $"/evals/{evaluation["id"]}");
            Console.WriteLine($"deleted evaluation {name}");
        }
    }
    await Foundry.Project(HttpMethod.Delete, $"/evaluationtaxonomies/{Foundry.AgentName}");
    Console.WriteLine($"deleted taxonomy {Foundry.AgentName}");
    await Foundry.Project(HttpMethod.Delete, $"/agents/{Foundry.AgentName}");
    Console.WriteLine($"deleted agent {Foundry.AgentName}");
}
