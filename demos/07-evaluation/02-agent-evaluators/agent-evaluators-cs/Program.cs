using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

DotNetEnv.Env.Load();

var projectEndpoint = Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!;
var accountEndpoint = projectEndpoint[..projectEndpoint.IndexOf("/api/projects/")].Replace(".services.ai.azure.com", ".openai.azure.com");
var judgeModel = Environment.GetEnvironmentVariable("JUDGE_MODEL_DEPLOYMENT_NAME")!;

IChatClient judgeClient = new AzureOpenAIClient(new Uri(accountEndpoint), new DefaultAzureCredential())
    .GetChatClient(judgeModel)
    .AsIChatClient();
var chatConfiguration = new ChatConfiguration(judgeClient);

var intentResolution = new IntentResolutionEvaluator();
var taskAdherence = new TaskAdherenceEvaluator();
var toolCallAccuracy = new ToolCallAccuracyEvaluator();

Console.WriteLine($"{"id",-3} {"scenario",-20} {"intent_resolution",-18} {"task_adherence",-15} {"tool_call_accuracy",-18}");

foreach (var line in File.ReadLines("agent_trajectories.jsonl"))
{
    using var document = JsonDocument.Parse(line);
    var root = document.RootElement;

    var query = ToMessages(root.GetProperty("query"));
    var response = new ChatResponse(ToMessages(root.GetProperty("response")));
    var tools = root.GetProperty("tool_definitions").EnumerateArray()
        .Select(tool => (AITool)AIFunctionFactory.CreateDeclaration(
            tool.GetProperty("name").GetString()!,
            tool.GetProperty("description").GetString(),
            tool.GetProperty("parameters").Clone()))
        .ToList();

    var intent = await intentResolution.EvaluateAsync(query, response, chatConfiguration, [new IntentResolutionEvaluatorContext(tools)]);
    var adherence = await taskAdherence.EvaluateAsync(query, response, chatConfiguration, [new TaskAdherenceEvaluatorContext(tools)]);
    var accuracy = await toolCallAccuracy.EvaluateAsync(query, response, chatConfiguration, [new ToolCallAccuracyEvaluatorContext(tools)]);

    Console.WriteLine(
        $"{root.GetProperty("id").GetString(),-3} {root.GetProperty("scenario").GetString(),-20} " +
        $"{Verdict(intent.Get<EvaluationMetric>(IntentResolutionEvaluator.IntentResolutionMetricName)),-18} " +
        $"{Verdict(adherence.Get<EvaluationMetric>(TaskAdherenceEvaluator.TaskAdherenceMetricName)),-15} " +
        $"{Verdict(accuracy.Get<EvaluationMetric>(ToolCallAccuracyEvaluator.ToolCallAccuracyMetricName)),-18}");
}

static string Verdict(EvaluationMetric metric)
{
    var value = metric switch
    {
        NumericMetric numeric => numeric.Value?.ToString(),
        BooleanMetric boolean => boolean.Value?.ToString(),
        _ => null,
    };
    if (value is null)
    {
        return "n/a";
    }

    return $"{(metric.Interpretation?.Failed == true ? "fail" : "pass")} ({value})";
}

static List<ChatMessage> ToMessages(JsonElement array)
{
    var messages = new List<ChatMessage>();
    foreach (var message in array.EnumerateArray())
    {
        var role = message.GetProperty("role").GetString();
        var content = message.GetProperty("content");
        if (content.ValueKind == JsonValueKind.String)
        {
            messages.Add(new ChatMessage(new ChatRole(role!), content.GetString()));
            continue;
        }

        var item = content[0];
        switch (item.GetProperty("type").GetString())
        {
            case "text":
                messages.Add(new ChatMessage(new ChatRole(role!), item.GetProperty("text").GetString()));
                break;
            case "tool_call":
                var arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(item.GetProperty("arguments").GetRawText());
                messages.Add(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(item.GetProperty("tool_call_id").GetString()!, item.GetProperty("name").GetString()!, arguments)]));
                break;
            case "tool_result":
                messages.Add(new ChatMessage(ChatRole.Tool, [new FunctionResultContent(message.GetProperty("tool_call_id").GetString()!, item.GetProperty("tool_result").Clone())]));
                break;
        }
    }

    return messages;
}
