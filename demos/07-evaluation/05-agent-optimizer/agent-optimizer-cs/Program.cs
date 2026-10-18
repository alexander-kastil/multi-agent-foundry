using System.Text.Json.Nodes;

switch (args.FirstOrDefault())
{
    case "baseline": await Baseline(); break;
    case "optimize": await Optimize(); break;
    case "show-candidates": await ShowCandidates(); break;
    case "promote": await Promote(); break;
    case "tool-tuning": await ToolTuning(); break;
    case "cleanup": await Cleanup(); break;
    default: Console.WriteLine("baseline | optimize | show-candidates | promote | tool-tuning | cleanup"); break;
}

static async Task Baseline()
{
    JsonNode agent = await Foundry.CreateAgentVersion(Foundry.BaselineInstructions);
    string version = agent["version"]!.GetValue<string>();
    Console.WriteLine($"agent {Foundry.AgentName} version {version}");
    (double score, int count) = await Foundry.ScoreVersion(version);
    Console.WriteLine($"baseline version {version}: mean f1 {score:0.00} on {count} tickets");
}

static async Task Optimize()
{
    string version = await Foundry.LatestVersion(Foundry.AgentName);
    var testCases = Foundry.LoadTickets().Select(ticket => (object)new { query = ticket.Query, ground_truth = ticket.GroundTruth });
    JsonNode created = await Foundry.CreateOptimizationJob(
        "m7c2 ticket triage", Foundry.AgentName, version, "instructions", 3, new { name = "builtin.f1_score" }, testCases, 4);
    string jobId = created["id"]!.GetValue<string>();
    Console.WriteLine($"optimization job {jobId}");
    File.WriteAllText("job_id.txt", jobId);

    JsonNode job = await Foundry.WaitForJob(jobId);
    if (job["status"]!.GetValue<string>() == "failed")
    {
        Console.WriteLine(job["error"]?["message"]?.GetValue<string>() ?? "no error message");
        return;
    }
    JsonNode summary = job["result"]!["candidate_summary"]!;
    Console.WriteLine($"baseline {summary["baseline_score"]!.GetValue<double>():0.0000}  best {summary["best_score"]!.GetValue<double>():0.0000}  best candidate {summary["best_id"]}");
    foreach (JsonNode candidate in await Foundry.Candidates(jobId))
    {
        Console.WriteLine($"{candidate["name"]}  {candidate["candidate_id"]}  {candidate["status"]}  score={candidate["evaluation"]?["score"]}");
    }
}

static async Task ShowCandidates()
{
    string jobId = File.ReadAllText("job_id.txt");
    JsonNode job = (await Foundry.Project(HttpMethod.Get, $"/agent_optimization_jobs/{jobId}"))!;
    JsonNode result = job["result"]!;
    JsonNode summary = result["candidate_summary"]!;
    Console.WriteLine($"job {job["status"]}, termination {result["termination_reason"]}");
    Console.WriteLine($"{"candidate",-12} {"score",7} {"delta",7} {"avg_tokens",11} {"latency_ms",11}");
    foreach (JsonNode candidate in (await Foundry.Candidates(jobId)).Where(item => item["evaluation"] is not null).OrderBy(item => item["name"]!.GetValue<string>()))
    {
        JsonNode evaluation = candidate["evaluation"]!;
        double score = evaluation["score"]!.GetValue<double>();
        string marker = candidate["candidate_id"]!.GetValue<string>() == summary["best_id"]!.GetValue<string>() ? " *" : "";
        Console.WriteLine($"{candidate["name"],-12} {score,7:0.000} {score - summary["baseline_score"]!.GetValue<double>(),7:+0.000;-0.000} {evaluation["avg_tokens"]!.GetValue<double>(),11:0} {evaluation["avg_latency_ms"]!.GetValue<double>(),11:0}{marker}");
    }
    foreach (JsonNode? usage in result["token_usage"]!.AsArray())
    {
        Console.WriteLine($"{usage!["stage"],-13} {usage["model"],-10} {usage["total_tokens"],8} tokens");
    }
}

static async Task Promote()
{
    string jobId = File.ReadAllText("job_id.txt");
    JsonNode job = (await Foundry.Project(HttpMethod.Get, $"/agent_optimization_jobs/{jobId}"))!;
    string bestId = job["result"]!["candidate_summary"]!["best_id"]!.GetValue<string>();
    JsonNode best = (await Foundry.Candidates(jobId, mutations: true)).First(candidate => candidate["candidate_id"]!.GetValue<string>() == bestId);
    foreach (JsonNode? mutation in best["output"]!["mutations"]!.AsArray())
    {
        Console.WriteLine($"{mutation!["type"]} mutation:");
        Console.WriteLine(mutation["value"]);
    }

    JsonNode promoted = (await Foundry.Project(HttpMethod.Post, $"/agent_optimization_jobs/{jobId}/candidates/{bestId}:promote"))!;
    Console.WriteLine($"promoted {promoted["name"]} to a new version of {Foundry.AgentName}");

    string version = await Foundry.LatestVersion(Foundry.AgentName);
    (double score, int count) = await Foundry.ScoreVersion(version);
    Console.WriteLine($"version {version}: mean f1 {score:0.00} on {count} tickets");
}

static object Tool(string name, string description) => new
{
    type = "function",
    name,
    description,
    parameters = new
    {
        type = "object",
        properties = new { value = new { type = "string", description = "The value." } },
        required = new[] { "value" },
        additionalProperties = false,
    },
    strict = true,
};

static async Task ToolTuning()
{
    const string gradeCode = """
        def grade(sample: dict, item: dict) -> float:
            expected_tool, expected_value = item.get("ground_truth", "|").split("|", 1)
            calls = item.get("tool_calls") or (item.get("sample") or {}).get("tool_calls") or []
            names = []
            values = []
            for message in calls:
                for part in message.get("content", []):
                    if part.get("type") == "function_call":
                        names.append(part.get("name"))
                        values.extend(str(value).lower() for value in (part.get("arguments") or {}).values())
            if expected_tool not in names:
                return 0.0
            return 0.5 + (0.5 if any(expected_value.lower() in value for value in values) else 0.0)
        """;

    (string Query, string GroundTruth)[] queries =
    [
        ("Where is my order A100?", "op_a1|A100"),
        ("Has order B207 shipped yet?", "op_a1|B207"),
        ("Is order C310 out for delivery?", "op_a1|C310"),
        ("How many loyalty points does anna@example.com have?", "op_a2|anna@example.com"),
        ("Which address is stored for the account of tom@example.com?", "op_a2|tom@example.com"),
        ("Is the account lea@example.com still active?", "op_a2|lea@example.com"),
        ("Do you sell a vegan truffle box?", "op_b2|vegan truffle"),
        ("Show me dark chocolate bars under 5 euro.", "op_b2|dark chocolate"),
        ("Do you have hazelnut pralines?", "op_b2|hazelnut"),
        ("When does the Vienna shop open on Sunday?", "op_c3|Vienna"),
        ("What time do you close in Graz today?", "op_c3|Graz"),
        ("Opening hours in Salzburg tomorrow?", "op_c3|Salzburg"),
    ];

    JsonNode agent = (await Foundry.Project(HttpMethod.Post, $"/agents/{Foundry.ToolAgentName}/versions", new
    {
        definition = new
        {
            kind = "prompt",
            model = Foundry.Model,
            instructions = "You are a support agent for a chocolate shop. Call a tool when it answers the question.",
            tools = new[]
            {
                Tool("op_a1", "Does a lookup."),
                Tool("op_a2", "Does another lookup."),
                Tool("op_b2", "Does a search."),
                Tool("op_c3", "Gets some data."),
            },
        },
    }))!;
    string agentVersion = agent["version"]!.GetValue<string>();
    Console.WriteLine($"agent {Foundry.ToolAgentName} version {agentVersion}");

    JsonNode evaluator = (await Foundry.Project(HttpMethod.Post, $"/evaluators/{Foundry.EvaluatorName}/versions", new
    {
        name = Foundry.EvaluatorName,
        categories = new[] { "quality" },
        display_name = "Tool choice",
        description = "Scores the tool name and the main argument of the first function call",
        definition = new
        {
            type = "code",
            code_text = gradeCode,
            init_parameters = new
            {
                type = "object",
                properties = new { deployment_name = new { type = "string" }, pass_threshold = new { type = "number" } },
                required = new[] { "deployment_name", "pass_threshold" },
            },
            metrics = new { result = new { type = "continuous", desirable_direction = "increase", min_value = 0.0, max_value = 1.0 } },
            data_schema = new
            {
                type = "object",
                required = new[] { "ground_truth", "tool_calls" },
                properties = new { ground_truth = new { type = "string" }, tool_calls = new { type = "array" } },
            },
        },
    }))!;
    string evaluatorVersion = evaluator["version"]!.GetValue<string>();
    Console.WriteLine($"evaluator {evaluator["name"]} version {evaluatorVersion}");

    var testCases = queries.Select(query => (object)new { query = query.Query, ground_truth = query.GroundTruth });
    var scorer = new
    {
        name = Foundry.EvaluatorName,
        version = evaluatorVersion,
        initialization_parameters = new { deployment_name = Foundry.EvalModel, pass_threshold = 0.5 },
    };
    JsonNode created = await Foundry.CreateOptimizationJob("m7c2 tool descriptions", Foundry.ToolAgentName, agentVersion, "tools", 2, scorer, testCases, null);
    string jobId = created["id"]!.GetValue<string>();
    Console.WriteLine($"optimization job {jobId}");

    JsonNode job = await Foundry.WaitForJob(jobId);
    if (job["status"]!.GetValue<string>() == "failed")
    {
        Console.WriteLine(job["error"]?["message"]?.GetValue<string>() ?? "no error");
        return;
    }
    JsonNode summary = job["result"]!["candidate_summary"]!;
    Console.WriteLine($"baseline {summary["baseline_score"]}  best {summary["best_score"]}");
    foreach (JsonNode candidate in await Foundry.Candidates(jobId, mutations: true))
    {
        Console.WriteLine($"{candidate["name"]}  {candidate["status"]}  score={candidate["evaluation"]?["score"]}");
        JsonArray mutations = candidate["output"]?["mutations"]?.AsArray() ?? [];
        foreach (JsonNode? mutation in mutations)
        {
            foreach (JsonNode? tool in mutation!["value"]!.AsArray())
            {
                JsonNode function = tool!["function"]!;
                string description = function["description"]!.GetValue<string>();
                Console.WriteLine($"  {function["name"]}: {description[..Math.Min(120, description.Length)]}");
            }
        }
    }
}

static async Task Cleanup()
{
    var jobEvaluations = new HashSet<string>();
    foreach (JsonNode job in await Foundry.List(query => Foundry.Project(HttpMethod.Get, $"/agent_optimization_jobs?{query}")))
    {
        string target = job["target_configuration"]?["name"]?.GetValue<string>() ?? "";
        if (target is Foundry.AgentName or Foundry.ToolAgentName)
        {
            await Foundry.Project(HttpMethod.Delete, $"/agent_optimization_jobs/{job["id"]}");
            jobEvaluations.Add($"agent_optimization_eval_{job["id"]!.GetValue<string>()[..8]}");
            Console.WriteLine($"deleted optimization job {job["id"]}");
        }
    }
    foreach (JsonNode evaluation in await Foundry.List(query => Foundry.OpenAI(HttpMethod.Get, $"/evals?{query}")))
    {
        string name = evaluation["name"]!.GetValue<string>();
        if (name.StartsWith(Foundry.EvaluationPrefix) || jobEvaluations.Contains(name))
        {
            await Foundry.OpenAI(HttpMethod.Delete, $"/evals/{evaluation["id"]}");
            Console.WriteLine($"deleted evaluation {name}");
        }
    }
    JsonNode versions = (await Foundry.Project(HttpMethod.Get, $"/evaluators/{Foundry.EvaluatorName}/versions"))!;
    foreach (JsonNode? version in versions["value"]?.AsArray() ?? versions["data"]?.AsArray() ?? [])
    {
        string number = version!["version"]!.GetValue<string>();
        await Foundry.Project(HttpMethod.Delete, $"/evaluators/{Foundry.EvaluatorName}/versions/{number}");
        Console.WriteLine($"deleted evaluator {Foundry.EvaluatorName} {number}");
    }
    JsonNode agents = (await Foundry.Project(HttpMethod.Get, "/agents"))!;
    var existing = agents["data"]!.AsArray().Select(agent => agent!["name"]!.GetValue<string>()).ToHashSet();
    foreach (string name in new[] { Foundry.AgentName, Foundry.ToolAgentName }.Where(existing.Contains))
    {
        await Foundry.Project(HttpMethod.Delete, $"/agents/{name}");
        Console.WriteLine($"deleted agent {name}");
    }
}
