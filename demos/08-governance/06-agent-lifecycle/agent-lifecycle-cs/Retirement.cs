using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.AI.Projects.Agents;

static class Retirement
{
    static readonly DateOnly Today = new(2026, 10, 18);

    static readonly JsonSerializerOptions Compact = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static async Task Run()
    {
        Manifest manifest = Manifest.Load("manifests/security-analyzer-2.2.0.yaml");
        Manifest replacement = Manifest.Load("manifests/security-analyzer-2.3.0.yaml");
        RetirementWindow retirement = new(DateOnly.Parse(manifest.Retirement!.DeprecatedDate), DateOnly.Parse(manifest.Retirement.DeletionDate), manifest.Retirement.ReplacementAgent);
        List<Tenant> tenants = Yaml.LoadTenants("tenants.yaml");

        ProjectsAgentVersion old = await Foundry.CreateAgent("m8h-life-retiring", manifest.PromptText(), new() { ["manifest_version"] = manifest.Version, ["status"] = "deprecated" });
        ProjectsAgentVersion updated = await Foundry.CreateAgent("m8h-life-retiring", replacement.PromptText(), new() { ["manifest_version"] = replacement.Version, ["status"] = "active" });
        Console.WriteLine($"Registered {old.Name} v{old.Version} (2.2.0, deprecated) and v{updated.Version} (2.3.0, active)");

        int noticeDays = retirement.Deletion.DayNumber - retirement.Deprecated.DayNumber;
        Console.WriteLine($"\nGovernance: {noticeDays} days between deprecation and deletion, minimum is 90: {(noticeDays >= 90 ? "ok" : "TOO SHORT")}");

        bool HasPin(Tenant t) => t.Pin is not null && t.Pin.AgentVersion == "2.2.0" && DateOnly.Parse(t.Pin.Expires) > Today;

        async Task<(int, JsonObject)> HandleRequest(Tenant tenant, string prompt)
        {
            int daysLeft = retirement.Deletion.DayNumber - Today.DayNumber;
            if (HasPin(tenant))
            {
                string answer = await Foundry.Ask(old, prompt);
                return (200, new JsonObject
                {
                    ["result"] = answer[..Math.Min(80, answer.Length)],
                    ["deprecation_warning"] = new JsonObject
                    {
                        ["message"] = $"This agent version is deprecated and will be deleted on {retirement.Deletion:yyyy-MM-dd}.",
                        ["migration_target"] = retirement.Replacement,
                        ["days_until_deletion"] = daysLeft,
                    },
                });
            }
            return (410, new JsonObject
            {
                ["error"] = "agent_deprecated",
                ["message"] = $"Security Analyzer 2.2.0 was deprecated on {retirement.Deprecated:yyyy-MM-dd} and will be deleted on {retirement.Deletion:yyyy-MM-dd}.",
                ["migration"] = new JsonObject { ["replacement"] = retirement.Replacement },
                ["deprecation_timeline"] = new JsonObject { ["days_remaining"] = daysLeft },
            });
        }

        Console.WriteLine("\nGraceful deprecation");
        foreach (Tenant tenant in tenants.Where(t => t.Id is "standard-northwind" or "standard-contoso"))
        {
            var (status, body) = await HandleRequest(tenant, "Is eval(user_input) a vulnerability?");
            string json = body.ToJsonString(Compact);
            Console.WriteLine($"  {tenant.Id}: HTTP {status} {json[..Math.Min(200, json.Length)]}");
        }

        Console.WriteLine("\nRetirement checklist");
        List<Tenant> pinned = tenants.Where(HasPin).ToList();
        string? latestPin = pinned.Select(t => t.Pin!.Expires).Order().LastOrDefault();
        Console.WriteLine($"  tenant version pins on 2.2.0: [{string.Join(", ", pinned.Select(t => $"'{t.Id}'"))}] (latest expiry {latestPin ?? "None"})");
        Console.WriteLine($"  replacement {replacement.Version} status: {replacement.Status}");

        Directory.CreateDirectory("out");
        JsonObject archive = new()
        {
            ["agent"] = manifest.AgentId,
            ["version"] = manifest.Version,
            ["archived_at"] = DateTime.UtcNow.ToString("o"),
            ["manifest"] = JsonSerializer.SerializeToNode(manifest),
        };
        File.WriteAllText("out/archive-security-analyzer-2.2.0.json", archive.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("  agent data archived to out/archive-security-analyzer-2.2.0.json");
        Console.WriteLine("  managed identity: disable now, delete after 90 days of audit retention");

        bool blocked = latestPin is not null && retirement.Deletion < DateOnly.Parse(latestPin) && pinned.Count > 0;
        Console.WriteLine($"\nDeletion date {retirement.Deletion:yyyy-MM-dd} vs pins: {(blocked ? "BLOCKED by pins" : "clear")}");

        Console.WriteLine("\nDeletion day (simulated): repoint the stable endpoint, then delete retired version 2.2.0 from Foundry");
        await Foundry.PinEndpoint(old.Name, updated.Version);
        Console.WriteLine($"  endpoint selector now serves v{await Foundry.ServedVersion(old.Name)} ({replacement.Version})");
        await Foundry.DeleteAgent(old);
        List<string> remaining = [];
        await foreach (ProjectsAgentVersion v in Foundry.Project.AgentAdministrationClient.GetAgentVersionsAsync(old.Name)) remaining.Add($"v{v.Version}");
        Console.WriteLine($"  remaining versions: [{string.Join(", ", remaining.Select(r => $"'{r}'"))}]");

        await Foundry.DeleteAgent(updated);
    }

    record RetirementWindow(DateOnly Deprecated, DateOnly Deletion, string Replacement);
}
