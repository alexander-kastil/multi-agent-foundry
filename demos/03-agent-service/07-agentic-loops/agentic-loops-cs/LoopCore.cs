using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using OpenAI.Responses;

class LoopCore(ProjectOpenAIClient openAi, AgentReference agent)
{
    public const string AgentName = "m3-investment-analyst";
    const int MaxReflectionCycles = 3;
    const int ConfidenceThreshold = 8;

    public const string Instructions =
        "You are an investment analyst for Contoso Capital. Always call get_market_data " +
        "before you make claims about a ticker. When asked for JSON, reply with raw JSON only, " +
        "without code fences or commentary.";

    public static readonly ResponseTool MarketDataTool = ResponseTool.CreateFunctionTool(
        functionName: "get_market_data",
        functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": { "ticker": { "type": "string", "description": "Ticker symbol such as MSFT" } },
              "required": ["ticker"],
              "additionalProperties": false
            }
            """),
        strictModeEnabled: true,
        functionDescription: "Get price, valuation and fundamentals for a stock ticker.");

    public async Task<ResponseResult> RunTurn(string input, string? conversationId = null, string? previousResponseId = null)
    {
        ProjectResponsesClient client = openAi.GetProjectResponsesClientForAgent(agent, conversationId);
        ResponseResult response = (await client.CreateResponseAsync(new CreateResponseOptions
        {
            InputItems = { ResponseItem.CreateUserMessageItem(input) },
            PreviousResponseId = previousResponseId,
        })).Value;
        while (true)
        {
            List<ResponseItem> outputs = response.OutputItems.OfType<FunctionCallResponseItem>()
                .Select(call => ResponseItem.CreateFunctionCallOutputItem(
                    call.CallId,
                    MarketData.Get(JsonNode.Parse(call.FunctionArguments)!["ticker"]!.GetValue<string>())))
                .Cast<ResponseItem>().ToList();
            if (outputs.Count == 0)
                return response;
            CreateResponseOptions followup = new() { PreviousResponseId = conversationId is null ? response.Id : null };
            foreach (ResponseItem output in outputs)
                followup.InputItems.Add(output);
            response = (await client.CreateResponseAsync(followup)).Value;
        }
    }

    public async Task<ResponseResult> PlanThenAct(string task)
    {
        ResponseResult planResponse = await RunTurn(
            """Decompose this task into research steps. Reply as JSON: {"steps": [{"id": 1, "action": "...", "tool": "..."}]}. """ + $"Task: {task}");
        JsonNode plan = JsonNode.Parse(planResponse.GetOutputText())!;
        Console.WriteLine($"Plan has {plan["steps"]!.AsArray().Count} steps");
        return await RunTurn(
            $"Execute this plan and give a buy, hold or sell recommendation: {plan.ToJsonString()}",
            previousResponseId: planResponse.Id);
    }

    public async Task<(ResponseResult Response, JsonNode Trace)> Reflect(ResponseResult response)
    {
        JsonNode trace = null!;
        for (int cycle = 1; cycle <= MaxReflectionCycles; cycle++)
        {
            response = await RunTurn(
                "Critique your analysis for unsupported assumptions, missing risks and weak comparisons, then revise it. " +
                """Reply as JSON: {"recommendation": "...", "reasoning_steps": ["..."], "confidence": 1-10, "revision_notes": "..."}""",
                previousResponseId: response.Id);
            trace = JsonNode.Parse(response.GetOutputText())!;
            int confidence = trace["confidence"]!.GetValue<int>();
            Console.WriteLine($"Reflection cycle {cycle}: confidence {confidence}");
            if (confidence >= ConfidenceThreshold)
                break;
        }
        return (response, trace);
    }

    async Task<ResponseResult> RunBackground(string prompt, string previousResponseId)
    {
        ProjectResponsesClient client = openAi.GetProjectResponsesClientForAgent(agent);
        ResponseResult job = (await client.CreateResponseAsync(new CreateResponseOptions
        {
            InputItems = { ResponseItem.CreateUserMessageItem(prompt) },
            PreviousResponseId = previousResponseId,
            BackgroundModeEnabled = true,
        })).Value;
        while (job.Status is ResponseStatus.Queued or ResponseStatus.InProgress)
        {
            await Task.Delay(2000);
            job = (await client.GetResponseAsync(job.Id)).Value;
        }
        return job;
    }

    public async Task<Dictionary<string, ResponseResult>> ForkScenarios(ResponseResult baseResponse)
    {
        Dictionary<string, string> branches = new()
        {
            ["bull"] = "Using the data already gathered and no new tool calls, assume 20 percent annual growth and write the bull-case thesis in three plain sentences.",
            ["bear"] = "Using the data already gathered and no new tool calls, assume 5 percent annual decline and write the bear-case thesis in three plain sentences.",
        };
        Dictionary<string, Task<ResponseResult>> jobs = branches.ToDictionary(branch => branch.Key, branch => RunBackground(branch.Value, baseResponse.Id));
        await Task.WhenAll(jobs.Values);
        return jobs.ToDictionary(job => job.Key, job => job.Value.Result);
    }
}
