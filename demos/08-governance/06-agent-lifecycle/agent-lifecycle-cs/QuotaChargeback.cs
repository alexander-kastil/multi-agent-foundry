using System.Diagnostics;
using Azure.AI.Projects.Agents;
using OpenAI.Responses;

record Tier(int TokensPerMinute, int MonthlyQuota);

record Admission(int Status, double RetryAfter = 0);

class TokenLimitGateway
{
    public const int WindowSeconds = 6;

    public static readonly Dictionary<string, Tier> Tiers = new()
    {
        ["premium"] = new(60000, 2000000),
        ["standard"] = new(25000, 750000),
        ["developer"] = new(150, 100000),
    };

    readonly Dictionary<string, double> windowStart = [];
    readonly Dictionary<string, int> windowTokens = [];
    readonly Dictionary<string, int> monthlyTokens = [];

    static double Now() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

    public Admission Admit(string tenant, string tier, string prompt)
    {
        Tier limits = Tiers[tier];
        double now = Now();
        if (now - windowStart.GetValueOrDefault(tenant) >= WindowSeconds)
        {
            windowStart[tenant] = now;
            windowTokens[tenant] = 0;
        }
        int estimate = prompt.Length / 4;
        if (monthlyTokens.GetValueOrDefault(tenant) + estimate > limits.MonthlyQuota) return new(403);
        int inWindow = windowTokens.GetValueOrDefault(tenant);
        if (inWindow + estimate > limits.TokensPerMinute || inWindow >= limits.TokensPerMinute) return new(429, WindowSeconds);
        return new(200);
    }

    public void Record(string tenant, int tokens)
    {
        windowTokens[tenant] = windowTokens.GetValueOrDefault(tenant) + tokens;
        monthlyTokens[tenant] = monthlyTokens.GetValueOrDefault(tenant) + tokens;
    }
}

class QuotaAwareAgentClient(TokenLimitGateway gateway, ProjectsAgentVersion agent, List<(string Tenant, int In, int Out)> usageLog, int maxRetries = 4)
{
    public async Task<string> Invoke(string tenant, string tier, string prompt)
    {
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            Admission admission = gateway.Admit(tenant, tier, prompt);
            if (admission.Status == 200)
            {
                ResponseResult response = (await Foundry.ForAgent(agent).CreateResponseAsync(prompt)).Value;
                gateway.Record(tenant, response.Usage.TotalTokenCount);
                usageLog.Add((tenant, response.Usage.InputTokenCount, response.Usage.OutputTokenCount));
                return response.GetOutputText();
            }
            if (admission.Status == 403) return "{'error': 'token_quota_exceeded'}";
            double delay = admission.RetryAfter * (0.5 + Random.Shared.NextDouble() * 0.5) * Math.Pow(2, attempt);
            Console.WriteLine($"    429 for {tenant}, attempt {attempt + 1}, retrying in {delay:F1}s");
            await Task.Delay(TimeSpan.FromSeconds(delay));
        }
        return $"{{'error': 'quota_exceeded_persistent', 'tenant_id': '{tenant}'}}";
    }
}

static class QuotaChargeback
{
    const double InputRatePer1K = 0.0025;
    const double OutputRatePer1K = 0.01;
    const double SharedComputeCost = 12.0;

    public static async Task Run()
    {
        ProjectsAgentVersion agent = await Foundry.CreateAgent("m8h-life-quota", "You answer security questions about source code in two sentences.");
        TokenLimitGateway gateway = new();
        List<(string Tenant, int In, int Out)> usageLog = [];
        QuotaAwareAgentClient client = new(gateway, agent, usageLog);

        (string Tenant, string Tier, string Prompt)[] workload =
        [
            ("premium-fabrikam-internal", "premium", "What is SQL injection?"),
            ("premium-fabrikam-internal", "premium", "What is cross-site scripting?"),
            ("standard-contoso", "standard", "What is a hardcoded credential?"),
            ("developer-startup", "developer", "What is insecure deserialization?"),
            ("developer-startup", "developer", "What is path traversal?"),
            ("developer-startup", "developer", "What is server-side request forgery?"),
        ];

        Console.WriteLine("Invocations through the token-limit gateway");
        foreach (var (tenant, tier, prompt) in workload)
        {
            Console.WriteLine($"  {tenant} ({tier}): {prompt}");
            string result = await client.Invoke(tenant, tier, prompt);
            Console.WriteLine($"    -> {result[..Math.Min(90, result.Length)]}");
        }

        Console.WriteLine("\nChargeback (model cost from metered tokens, shared compute split evenly across active tenants)");
        var byTenant = usageLog.GroupBy(u => u.Tenant).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => (Tenant: g.Key, In: g.Sum(u => u.In), Out: g.Sum(u => u.Out))).ToList();
        double shared = SharedComputeCost / byTenant.Count;
        double total = 0;
        foreach (var (tenant, tokensIn, tokensOut) in byTenant)
        {
            double modelCost = tokensIn / 1000.0 * InputRatePer1K + tokensOut / 1000.0 * OutputRatePer1K;
            total += modelCost + shared;
            Console.WriteLine($"  {tenant,-26} in={tokensIn,5} out={tokensOut,5} model=${modelCost:F4} shared=${shared:F2} total=${modelCost + shared:F4}");
        }
        Console.WriteLine($"  total allocated: ${total:F4}");

        await Foundry.DeleteAgent(agent);
    }
}
