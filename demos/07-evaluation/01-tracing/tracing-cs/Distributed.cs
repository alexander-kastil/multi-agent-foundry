using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;

static class Distributed
{
    const string RouterPrompt =
        "Classify the customer message. Reply with JSON only and no markdown: " +
        "{\"target_agent\": \"product-search-agent\" or \"order-agent\" or \"payment-agent\", \"confidence\": 0.0 to 1.0}.";

    static readonly string[] Requests =
    [
        "Do you have sugar-free dark chocolate under 20 dollars?",
        "Where is my order 12345?",
        "I want to update the credit card on my account.",
    ];

    static readonly Dictionary<string, string> Specialists = new()
    {
        ["m7c1-product-search-agent"] = "You are a chocolate shop product search assistant. Answer in two sentences.",
        ["m7c1-order-agent"] = "You are an order status assistant. Invent plausible tracking details. Answer in two sentences.",
    };

    public static async Task Run()
    {
        string model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
        AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
        string connectionString = await project.Telemetry.GetApplicationInsightsConnectionStringAsync();

        SpanCollector collector = new();
        using TracerProvider provider = Telemetry.Configure(collector, connectionString);
        using ILoggerFactory loggers = LoggerFactory.Create(b => b.AddOpenTelemetry(o =>
        {
            o.IncludeFormattedMessage = true;
            o.AddAzureMonitorLogExporter(x => x.ConnectionString = connectionString);
        }));
        ILogger logger = loggers.CreateLogger("agent-decisions");
        StructuredLogger orchestratorLog = new("orchestrator-agent", "1.0.0", logger);
        ActivitySource tracer = new("orchestrator");

        Dictionary<string, string> versions = [];
        foreach ((string name, string instructions) in Specialists)
        {
            ProjectsAgentVersion agent = await project.AgentAdministrationClient.CreateAgentVersionAsync(
                name, new ProjectsAgentVersionCreationOptions(new DeclarativeAgentDefinition(model) { Instructions = instructions }));
            versions[name] = agent.Version;
        }

        int port = FreePort();
        string baseUrl = $"http://127.0.0.1:{port}";
        HttpListener server = DownstreamAgent.Start(project, versions, logger, $"{baseUrl}/");
        using HttpClient http = new() { Timeout = TimeSpan.FromSeconds(120) };
        ProjectResponsesClient router = project.ProjectOpenAIClient.GetProjectResponsesClientForModel(model);

        for (int index = 0; index < Requests.Length; index++)
        {
            string question = Requests[index];
            string sessionId = $"session-{index}";
            using Activity? root = tracer.StartActivity("customer_request");
            root?.SetTag("session.id", sessionId);

            JsonNode decision;
            using (Activity? routing = tracer.StartActivity($"chat {model}"))
            {
                routing?.SetTag("gen_ai.operation.name", "chat");
                routing?.SetTag("gen_ai.agent.name", "orchestrator-agent");
                ResponseResult response = (await router.CreateResponseAsync(new CreateResponseOptions
                {
                    Instructions = RouterPrompt,
                    InputItems = { ResponseItem.CreateUserMessageItem(question) },
                })).Value;
                decision = JsonNode.Parse(response.GetOutputText())!;
                orchestratorLog.Log("LLM_ROUTING", "success", sessionId, "premium", response.Usage.InputTokenCount, response.Usage.OutputTokenCount, output: decision);
            }

            string target = "m7c1-" + (string)decision["target_agent"]!;
            using (Activity? call = tracer.StartActivity($"orchestrator_to_{target}", ActivityKind.Client))
            {
                call?.SetTag("gen_ai.agent.name", "orchestrator-agent");
                call?.SetTag("agent.downstream", target);
                HttpResponseMessage reply = await http.PostAsJsonAsync($"{baseUrl}/{target}", new { input = question, session_id = sessionId, customer_tier = "premium" });
                JsonNode body = JsonNode.Parse(await reply.Content.ReadAsStringAsync())!;
                if (reply.IsSuccessStatusCode)
                {
                    Console.WriteLine($"{target}: {body["answer"]}");
                }
                else
                {
                    call?.SetStatus(ActivityStatusCode.Error);
                    orchestratorLog.Log("AGENT_CALL", "error", errorCode: (string?)body["error"]);
                }
            }
        }

        server.Stop();
        foreach ((string name, string version) in versions)
            await project.AgentAdministrationClient.DeleteAgentVersionAsync(name, version);

        provider.ForceFlush();
        loggers.Dispose();
        Console.WriteLine();
        Console.WriteLine(collector.Waterfall());
        Console.WriteLine();
        Console.WriteLine($"Requests: {Requests.Length}  Distinct trace IDs: {collector.TraceIds().Count}");
        Console.WriteLine($"Flagged spans: [{string.Join(", ", collector.Flagged().Select(s => $"'{s.DisplayName}'"))}]");
    }

    static int FreePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
