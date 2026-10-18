using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

static class DownstreamAgent
{
    static readonly ActivitySource Tracer = new("downstream-agents");

    public static HttpListener Start(AIProjectClient project, Dictionary<string, string> agents, ILogger logger, string prefix)
    {
        HttpListener listener = new();
        listener.Prefixes.Add(prefix);
        listener.Start();
        _ = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext context;
                try { context = await listener.GetContextAsync(); }
                catch (HttpListenerException) { break; }
                _ = Task.Run(() => Handle(context, project, agents, logger));
            }
        });
        return listener;
    }

    static async Task Handle(HttpListenerContext context, AIProjectClient project, Dictionary<string, string> agents, ILogger logger)
    {
        string agentName = context.Request.Url!.AbsolutePath.Trim('/');
        using StreamReader reader = new(context.Request.InputStream);
        JsonNode body = JsonNode.Parse(await reader.ReadToEndAsync())!;
        ActivityContext parent = Propagation.Extract((WebHeaderCollection)context.Request.Headers);

        using Activity? span = Tracer.StartActivity($"invoke_agent {agentName}", ActivityKind.Server, parent);
        span?.SetTag("gen_ai.operation.name", "invoke_agent");
        span?.SetTag("gen_ai.agent.name", agentName);

        if (!agents.TryGetValue(agentName, out string? version))
        {
            new StructuredLogger(agentName, "0", logger).Log("AGENT_CALL", "error", errorCode: "agent_not_found");
            await Respond(context, 404, new { error = "agent_not_found" });
            return;
        }

        long started = Stopwatch.GetTimestamp();
        ResponseResult response;
        long latencyMs;
        using (Activity? llm = Tracer.StartActivity("chat"))
        {
            ProjectResponsesClient client = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(agentName, version));
            response = (await client.CreateResponseAsync((string)body["input"]!)).Value;
            latencyMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            llm?.SetTag("gen_ai.operation.name", "chat");
            llm?.SetTag("gen_ai.agent.name", agentName);
            llm?.SetTag("gen_ai.usage.input_tokens", response.Usage.InputTokenCount);
            llm?.SetTag("gen_ai.usage.output_tokens", response.Usage.OutputTokenCount);
        }
        new StructuredLogger(agentName, version, logger).Log("LLM_CALL", "success", (string?)body["session_id"], (string?)body["customer_tier"],
            response.Usage.InputTokenCount, response.Usage.OutputTokenCount, latencyMs);
        await Respond(context, 200, new { answer = response.GetOutputText() });
    }

    static async Task Respond(HttpListenerContext context, int status, object payload)
    {
        byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = data.Length;
        await context.Response.OutputStream.WriteAsync(data);
        context.Response.Close();
    }
}
