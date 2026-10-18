using System.Text.Json.Nodes;
using Microsoft.Agents.AI;

static class Planner
{
    const string PlannerInstructions = "You are a research task decomposition planner. Reply with raw JSON only.";

    const string PlanPrompt = """
        Research query: "{0}"

        Available specialist agents:
        {1}

        Return JSON: {{"complexity_assessment": "simple|moderate|complex", "subtasks": [{{"task_id": "T1", "description": "...", "assigned_agent": "registry name", "depends_on": []}}]}}

        Rules: include only necessary subtasks, tasks without dependencies can run in parallel, simpler queries need fewer subtasks, a complex query ends with investment_thesis_generator.
        """;

    static readonly string[] Queries =
    [
        "What is Microsoft's current stock price?",
        "Should Contoso Capital invest in Microsoft given current AI market trends and macroeconomic conditions?",
    ];

    static Task<JsonNode> CreatePlan(string query) =>
        Agents.AskJson(
            Agents.Make("meta_agent_planner", PlannerInstructions),
            string.Format(PlanPrompt, query, Registry.AgentRegistry.ToJsonString(new() { WriteIndented = true })));

    static async Task<string> ExecuteTask(JsonNode task, Dictionary<string, string> context, string query)
    {
        string agentName = task["assigned_agent"]!.GetValue<string>();
        string capabilities = Registry.AgentRegistry[agentName]!["capabilities"]!.ToJsonString();
        AIAgent specialist = Agents.Make(
            agentName,
            $"You are the {agentName} specialist with capabilities: {capabilities}. Answer in at most three sentences and invent plausible illustrative figures when you have no data.");
        string contextText = context.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(context) : "none";
        return (await specialist.RunAsync($"Original query: {query}\nTask: {task["description"]}\nContext from prior tasks: {contextText}")).Text;
    }

    public static async Task<Dictionary<string, string>> ExecutePlan(JsonNode plan, string query)
    {
        Dictionary<string, JsonNode> pending = plan["subtasks"]!.AsArray().ToDictionary(task => task!["task_id"]!.GetValue<string>(), task => task!);
        Dictionary<string, string> completed = [];
        int wave = 0;
        while (pending.Count > 0)
        {
            List<JsonNode> ready = pending.Values
                .Where(task => task["depends_on"]!.AsArray().All(dep => completed.ContainsKey(dep!.GetValue<string>())))
                .ToList();
            if (ready.Count == 0)
                throw new InvalidOperationException($"Plan has a dependency cycle or an unknown dependency among [{string.Join(", ", pending.Keys.Select(id => $"'{id}'"))}]");
            wave++;
            Console.WriteLine($"Wave {wave}: [{string.Join(", ", ready.Select(task => $"'{task["task_id"]}'"))}]");
            string[] results = await Task.WhenAll(ready.Select(task => ExecuteTask(
                task,
                task["depends_on"]!.AsArray().ToDictionary(dep => dep!.GetValue<string>(), dep => completed[dep!.GetValue<string>()]),
                query)));
            for (int i = 0; i < ready.Count; i++)
            {
                string id = ready[i]["task_id"]!.GetValue<string>();
                completed[id] = results[i];
                pending.Remove(id);
            }
        }
        return completed;
    }

    public static async Task Run()
    {
        foreach (string query in Queries)
        {
            JsonNode plan = await CreatePlan(query);
            JsonArray subtasks = plan["subtasks"]!.AsArray();
            Console.WriteLine($"\nQuery: {query}\nComplexity: {plan["complexity_assessment"]}, subtasks: {subtasks.Count}");
            foreach (JsonNode? task in subtasks)
                Console.WriteLine($"  {task!["task_id"]} -> {task["assigned_agent"]} depends_on={task["depends_on"]!.ToJsonString()}");
            Dictionary<string, string> completed = await ExecutePlan(plan, query);
            Console.WriteLine($"Final output: {completed[subtasks[^1]!["task_id"]!.GetValue<string>()]}");
        }
    }

    public static async Task CycleCheck()
    {
        JsonNode cyclic = JsonNode.Parse("""
            {
              "subtasks": [
                { "task_id": "T1", "description": "Get prices", "assigned_agent": "financial_data_retriever", "depends_on": ["T2"] },
                { "task_id": "T2", "description": "Analyze filings", "assigned_agent": "sec_filing_analyzer", "depends_on": ["T1"] }
              ]
            }
            """)!;
        try
        {
            await ExecutePlan(cyclic, "Should we invest?");
        }
        catch (InvalidOperationException error)
        {
            Console.WriteLine(error.Message);
        }
    }
}
