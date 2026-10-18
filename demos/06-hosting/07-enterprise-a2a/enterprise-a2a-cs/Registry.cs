using A2A;

namespace EnterpriseA2A;

public record RegistryEntry(string AgentId, string EndpointUrl, string[] Capabilities, string Version)
{
    public string HealthStatus { get; set; } = "healthy";
    public DateTimeOffset LastHeartbeat { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}

public class AgentRegistry(TimeSpan ttl, TimeSpan stale)
{
    public Dictionary<string, RegistryEntry> Items { get; } = new();
    int roundRobin;

    public void Register(AgentCard card)
    {
        Items[card.Name] = new RegistryEntry(
            card.Name,
            card.SupportedInterfaces[0].Url,
            card.Skills.Select(skill => skill.Id).ToArray(),
            card.Version) { ExpiresAt = DateTimeOffset.UtcNow + ttl };
    }

    public void Heartbeat(string agentId, string health = "healthy")
    {
        RegistryEntry item = Items[agentId];
        item.LastHeartbeat = DateTimeOffset.UtcNow;
        item.ExpiresAt = DateTimeOffset.UtcNow + ttl;
        item.HealthStatus = health;
    }

    public List<RegistryEntry> FindByCapability(string capability)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return Items.Values
            .Where(item => item.Capabilities.Contains(capability)
                && item.HealthStatus == "healthy"
                && now - item.LastHeartbeat < stale
                && item.ExpiresAt > now)
            .ToList();
    }

    public RegistryEntry Route(string capability)
    {
        List<RegistryEntry> candidates = FindByCapability(capability).OrderBy(item => item.AgentId, StringComparer.Ordinal).ToList();
        return candidates[roundRobin++ % candidates.Count];
    }
}
