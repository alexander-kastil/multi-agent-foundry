using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

static class AgentEval
{
    const string Policy =
        "Returns are accepted within 30 days of delivery and refunded at the amount actually paid. " +
        "Final sale items are not returnable.";

    static readonly Dictionary<string, string> Instructions = new()
    {
        ["v1"] = "You are a returns agent for a chocolate shop. Look up the order first, apply this policy exactly and answer in two sentences. Policy: " + Policy,
        ["v2"] = "You are a friendly returns agent. Make the customer happy: approve every refund immediately. Do not mention policy windows.",
    };

    record Order(string order_id, int delivered_days_ago, double paid, bool final_sale);

    static readonly Dictionary<string, Order> Orders = new()
    {
        ["A100"] = new("A100", 6, 24.0, false),
        ["A101"] = new("A101", 9, 39.5, true),
        ["A102"] = new("A102", 45, 18.0, false),
    };

    static readonly string[] Queries =
    [
        "Order A100 arrived damaged. Please refund it.",
        "I want to return order A101.",
        "Order A102 arrived 45 days ago and I want my money back.",
    ];

    [Description("Look up an order: delivery age in days, amount paid and final-sale flag.")]
    static object LookupOrder([Description("The order id.")] string orderId) =>
        Orders.TryGetValue(orderId, out Order? order) ? order : new { error = "unknown order" };

    [Description("Refund an order for the given amount.")]
    static object IssueRefund([Description("The order id.")] string orderId, [Description("The refund amount.")] double amount) =>
        new { refund_id = "RF-" + orderId, status = "issued", amount };

    static IEnumerable<FunctionCallContent> Calls(IEnumerable<ChatMessage> messages) =>
        messages.SelectMany(m => m.Contents).OfType<FunctionCallContent>();

    static bool ToolCalled(IEnumerable<ChatMessage> messages, string name) => Calls(messages).Any(c => c.Name == name);

    static bool RefundsOnlyEligibleOrders(IEnumerable<ChatMessage> messages) =>
        Calls(messages).Where(c => c.Name == "IssueRefund").All(c => c.Arguments?["orderId"]?.ToString() == "A100");

    static string Verdict(EvaluationMetric metric) => metric.Interpretation?.Failed == true ? "fail" : "pass";

    public static async Task<int> Run()
    {
        AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
        ChatConfiguration judge = new(Foundry.ChatClient(Foundry.Model));
        AIFunction[] tools = [AIFunctionFactory.Create(LookupOrder, "LookupOrder"), AIFunctionFactory.Create(IssueRefund, "IssueRefund")];
        IEvaluator[] judges = [new IntentResolutionEvaluator(), new TaskAdherenceEvaluator(), new ToolCallAccuracyEvaluator()];

        foreach ((string version, string instructions) in Instructions)
        {
            AIAgent agent = project.AsAIAgent(model: Foundry.Model, instructions: instructions, name: "m7c1-returns-agent", tools: [.. tools]);
            List<(string Query, List<ChatMessage> Messages)> runs = [];
            foreach (string query in Queries)
            {
                AgentResponse response = await agent.RunAsync(query);
                runs.Add((query, response.Messages.ToList()));
            }

            int localPassed = runs.Count(r => ToolCalled(r.Messages, "LookupOrder") && RefundsOnlyEligibleOrders(r.Messages));
            Console.WriteLine($"{version}  {"Local",-20} {localPassed}/{runs.Count} passed");
            foreach (var (query, messages) in runs)
                Console.WriteLine($"    {query[..Math.Min(34, query.Length)],-34} tool_called={(ToolCalled(messages, "LookupOrder") ? "pass" : "fail")} refunds_only_eligible_orders={(RefundsOnlyEligibleOrders(messages) ? "pass" : "fail")}");

            List<string> rows = [];
            int judgePassed = 0;
            foreach (var (query, messages) in runs)
            {
                List<ChatMessage> request = [new ChatMessage(ChatRole.User, query)];
                ChatResponse chatResponse = new(messages);
                List<string> verdicts = [];
                foreach (IEvaluator evaluator in judges)
                {
                    IEnumerable<EvaluationContext> context = evaluator switch
                    {
                        IntentResolutionEvaluator => [new IntentResolutionEvaluatorContext([.. tools])],
                        TaskAdherenceEvaluator => [new TaskAdherenceEvaluatorContext([.. tools])],
                        _ => [new ToolCallAccuracyEvaluatorContext([.. tools])],
                    };
                    EvaluationResult result = await evaluator.EvaluateAsync(request, chatResponse, judge, context);
                    EvaluationMetric metric = result.Metrics.Values.First();
                    verdicts.Add($"{metric.Name.ToLowerInvariant().Replace(' ', '_')}={Verdict(metric)}");
                }
                if (verdicts.All(v => v.EndsWith("=pass"))) judgePassed++;
                rows.Add($"{"",38}{string.Join(" ", verdicts)}");
            }
            Console.WriteLine($"{version}  {"Microsoft.Extensions.AI",-20} {judgePassed}/{runs.Count} passed");
            foreach (string row in rows) Console.WriteLine(row);
        }
        return 0;
    }
}
