using System.ComponentModel;
using System.Diagnostics;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenTelemetry.Trace;

static class AgentTrace
{
    static readonly Dictionary<string, string> Orders = new()
    {
        ["12345"] = "shipped on 2026-10-14, arriving 2026-10-19",
        ["98765"] = "processing",
    };

    [Description("Look up the status of a customer order.")]
    static string GetOrderStatus([Description("The order id.")] string orderId) =>
        Orders.GetValueOrDefault(orderId, "unknown order");

    public static async Task Run()
    {
        SpanCollector collector = new();
        bool export = Environment.GetEnvironmentVariable("EXPORT_TO_APP_INSIGHTS")?.ToLowerInvariant() == "true";
        AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
        string? connectionString = export ? await project.Telemetry.GetApplicationInsightsConnectionStringAsync() : null;

        using TracerProvider provider = Telemetry.Configure(collector, connectionString);

        AIAgent agent = project
            .AsAIAgent(
                model: Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!,
                instructions: "You answer order questions in one sentence. Use the tool for order status.",
                name: "order-agent",
                tools: [AIFunctionFactory.Create(GetOrderStatus, "get_order_status")],
                clientFactory: chatClient => chatClient.AsBuilder().UseOpenTelemetry(sourceName: "agent-framework").Build())
            .AsBuilder()
            .UseOpenTelemetry(sourceName: "agent-framework")
            .Build();

        using ActivitySource source = new("orchestrator");
        AgentResponse result;
        using (source.StartActivity("customer_request"))
        {
            result = await agent.RunAsync("Where is my order 12345?");
        }
        Console.WriteLine(result.Text);

        provider.ForceFlush();
        Console.WriteLine();
        Console.WriteLine(collector.Waterfall());
    }
}
