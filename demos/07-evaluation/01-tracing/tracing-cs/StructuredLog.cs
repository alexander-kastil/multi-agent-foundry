using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

class StructuredLogger(string agentId, string agentVersion, ILogger logger)
{
    public void Log(string operationType, string status, string? sessionId = null, string? customerTier = null,
        long inputTokens = 0, long outputTokens = 0, long latencyMs = 0, string? errorCode = null, JsonNode? output = null)
    {
        Activity? span = Activity.Current;
        JsonObject entry = new()
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz"),
            ["trace_id"] = span?.TraceId.ToString(),
            ["span_id"] = span?.SpanId.ToString(),
            ["agent_id"] = agentId,
            ["agent_version"] = agentVersion,
            ["operation_type"] = operationType,
            ["status"] = status,
            ["session_id_hash"] = sessionId is null ? null : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(sessionId)))[..16],
            ["customer_tier"] = customerTier,
            ["input_token_count"] = inputTokens,
            ["output_token_count"] = outputTokens,
            ["latency_ms"] = latencyMs,
            ["error_code"] = errorCode,
            ["output"] = output,
        };
        foreach (string key in entry.Where(e => e.Value is null).Select(e => e.Key).ToList()) entry.Remove(key);
        string json = entry.ToJsonString(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        Console.WriteLine(json);
        if (status == "error") logger.LogError("{Entry}", json);
        else logger.LogInformation("{Entry}", json);
        span?.SetTag($"log.{operationType}.status", status);
        if (errorCode is not null)
        {
            span?.SetStatus(ActivityStatusCode.Error);
            span?.SetTag("error.code", errorCode);
        }
    }
}
