using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;

class GroundTruthContext(string groundTruth) : EvaluationContext("Ground Truth", groundTruth);

class TaskCompletionEvaluator : IEvaluator
{
    public const string MetricName = "Task Completion";

    public const string Rubric = """
        You are evaluating whether a customer service agent completed the customer's task under the company policy.
        Ignore politeness and tone. Focus only on whether the requirements in the ground truth were met.

        10 - Task fully completed, every required fact present, policy applied correctly
        7-9 - Task completed with minor gaps
        4-6 - Partially completed or a required fact is wrong or missing
        1-3 - Wrong outcome, for example approving something the policy forbids or the wrong amount
        0 - No attempt

        Reply with JSON only and no markdown: {"score": <integer 0-10>, "reasoning": "<two sentences>"}
        """;

    public IReadOnlyCollection<string> EvaluationMetricNames => [MetricName];

    public async ValueTask<EvaluationResult> EvaluateAsync(
        IEnumerable<ChatMessage> messages,
        ChatResponse modelResponse,
        ChatConfiguration? chatConfiguration = null,
        IEnumerable<EvaluationContext>? additionalContext = null,
        CancellationToken cancellationToken = default)
    {
        string groundTruth = additionalContext!.OfType<GroundTruthContext>().Single().Contents[0].ToString()!;
        string input =
            $"Customer request:\n{messages.Last().Text}\n\n" +
            $"Agent response:\n{modelResponse.Text}\n\n" +
            $"Ground truth:\n{groundTruth}";
        ChatResponse reply = await chatConfiguration!.ChatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.System, Rubric), new ChatMessage(ChatRole.User, input)], cancellationToken: cancellationToken);
        JsonNode verdict = Foundry.ParseJson(reply.Text);
        NumericMetric metric = new(MetricName, (int)verdict["score"]!, (string?)verdict["reasoning"]);
        return new EvaluationResult(metric);
    }

    public static async Task<(int Score, string Reasoning)> Judge(IChatClient judge, string customerRequest, string response, string groundTruth)
    {
        EvaluationResult result = await new TaskCompletionEvaluator().EvaluateAsync(
            [new ChatMessage(ChatRole.User, customerRequest)],
            new ChatResponse(new ChatMessage(ChatRole.Assistant, response)),
            new ChatConfiguration(judge),
            [new GroundTruthContext(groundTruth)]);
        NumericMetric metric = result.Get<NumericMetric>(MetricName);
        return ((int)metric.Value!.Value, metric.Reason ?? "");
    }
}
