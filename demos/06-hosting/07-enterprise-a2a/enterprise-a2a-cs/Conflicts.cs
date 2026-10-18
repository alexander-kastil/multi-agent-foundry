using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace EnterpriseA2A;

public record ConflictReport(bool Conflict, string Type, string Details, string AgentA, string AgentB, double Confidence);

public record Resolution(string Outcome, string Method, string? ChosenAgent, bool RequiresHumanReview = false);

public record AuditEntry(DateTimeOffset Timestamp, string TenantId, string ConflictType, string[] Agents, double Confidence, string Resolution, string Method, string? ChosenAgent);

public static class Conflicts
{
    static readonly Dictionary<string, int> AgentPriority = new()
    {
        ["compliance"] = 100,
        ["risk"] = 75,
        ["market-analysis"] = 50,
        ["reporting"] = 25,
    };

    const string JudgeInstructions =
        "You evaluate whether two financial agent analyses are consistent. Reply with raw JSON only: " +
        "{\"status\": \"CONSISTENT\" | \"INCONSISTENT\" | \"COMPLEMENTARY\", \"reasoning\": \"...\", \"confidence\": 0.0-1.0}";

    static readonly HashSet<string> Up = ["increase", "buy"];
    static readonly HashSet<string> Down = ["decrease", "sell"];

    static string? Action(JsonObject output) => output["recommendation"]?["action"]?.GetValue<string>();

    static ConflictReport? RuleBased(string agentA, JsonObject a, string agentB, JsonObject b)
    {
        string? actionA = Action(a);
        string? actionB = Action(b);
        if (actionA is null || actionB is null) return null;
        bool contradictory = (Up.Contains(actionA) && Down.Contains(actionB)) || (Down.Contains(actionA) && Up.Contains(actionB));
        return contradictory
            ? new ConflictReport(true, "rule-based", $"contradictory-actions: {actionA} vs {actionB}", agentA, agentB, 1.0)
            : null;
    }

    public static async Task<ConflictReport> DetectAsync(IChatClient chat, string agentA, JsonObject a, string agentB, JsonObject b)
    {
        ConflictReport? rule = RuleBased(agentA, a, agentB, b);
        if (rule is not null) return rule;

        ChatResponse response = await chat.GetResponseAsync(
            [
                new ChatMessage(ChatRole.System, JudgeInstructions),
                new ChatMessage(ChatRole.User, $"Agent A:\n{a.ToJsonString()}\n\nAgent B:\n{b.ToJsonString()}"),
            ]);
        JsonNode judged = JsonNode.Parse(response.Text)!;
        return new ConflictReport(
            judged["status"]!.GetValue<string>() == "INCONSISTENT",
            "semantic",
            judged["reasoning"]!.GetValue<string>(),
            agentA,
            agentB,
            judged["confidence"]!.GetValue<double>());
    }

    static string AgentType(string agentId) => agentId.Split("-v")[0];

    public static Resolution Resolve(ConflictReport report)
    {
        int priorityA = AgentPriority.GetValueOrDefault(AgentType(report.AgentA));
        int priorityB = AgentPriority.GetValueOrDefault(AgentType(report.AgentB));
        if (priorityA == priorityB)
            return new Resolution("escalated", "orchestrator-synthesis", null, report.Confidence < 0.5);
        return new Resolution("resolved", "priority-based", priorityA > priorityB ? report.AgentA : report.AgentB);
    }
}

public class AuditTrail
{
    public List<AuditEntry> Entries { get; } = new();

    public void Log(string tenantId, ConflictReport report, Resolution resolution) =>
        Entries.Add(new AuditEntry(DateTimeOffset.UtcNow, tenantId, report.Type, [report.AgentA, report.AgentB], report.Confidence, resolution.Outcome, resolution.Method, resolution.ChosenAgent));
}
