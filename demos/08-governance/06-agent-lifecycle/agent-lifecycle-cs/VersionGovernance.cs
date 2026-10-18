using System.Security.Cryptography;
using Azure.AI.Projects.Agents;

static class VersionGovernance
{
    static readonly Dictionary<int, int> ValidationDays = new() { [1] = 0, [2] = 7, [3] = 30 };

    static (string Verdict, List<string> Reasons) Classify(Manifest old, Manifest updated)
    {
        List<string> reasons = [];
        if (old.Model.DeploymentName != updated.Model.DeploymentName) reasons.Add("model deployment changed");
        double ratio = SequenceMatcher.Ratio(old.PromptText(), updated.PromptText());
        if (1 - ratio > 0.20) reasons.Add($"system prompt rewritten ({(1 - ratio) * 100:0}% of text modified)");
        if (updated.Tools.Select(t => t.Name).Except(old.Tools.Select(t => t.Name)).Any()) reasons.Add("new tool integration");
        foreach ((string metric, double value) in old.EvaluationBaseline)
            if (updated.EvaluationBaseline[metric] < value) reasons.Add($"evaluation baseline reduced ({metric})");
        return reasons.Count > 0 ? ("MANUAL approval", reasons) : ("AUTOMATED approval", ["tuning only"]);
    }

    static string Eligible(Tenant tenant, DateOnly approvedOn, DateOnly today)
    {
        if (tenant.Pin is not null && DateOnly.Parse(tenant.Pin.Expires) > today)
            return $"held on {tenant.Pin.AgentVersion} (pin until {tenant.Pin.Expires})";
        int wait = ValidationDays[tenant.RolloutTier];
        return today >= approvedOn.AddDays(wait) ? "deploy 2.3.0" : $"wait until {approvedOn.AddDays(wait):yyyy-MM-dd}";
    }

    public static async Task Run()
    {
        Manifest old = Manifest.Load("manifests/security-analyzer-2.2.0.yaml");
        Manifest updated = Manifest.Load("manifests/security-analyzer-2.3.0.yaml");

        Console.WriteLine("Manifest integrity (prompt file hash must match manifest)");
        foreach (Manifest m in new[] { old, updated })
        {
            string actual = Convert.ToHexStringLower(SHA256.HashData(m.PromptBytes()));
            Console.WriteLine($"  {m.Version}: {(actual == m.SystemPrompt.Hash ? "match" : "DRIFT")}");
        }

        Console.WriteLine("\nApproval gate");
        var (verdict, reasons) = Classify(old, updated);
        Console.WriteLine($"  2.2.0 -> 2.3.0: {verdict} ({string.Join("; ", reasons)})");
        Manifest tuned = Manifest.Load("manifests/security-analyzer-2.2.0.yaml");
        tuned.Model.Temperature = 0.28;
        (verdict, reasons) = Classify(old, tuned);
        Console.WriteLine($"  2.2.0 -> temperature 0.28: {verdict} ({string.Join("; ", reasons)})");

        Console.WriteLine("\nCompliance fields in the 2.3.0 manifest");
        Console.WriteLine($"  eu_ai_act_risk_classification: {updated.EuAiActRiskClassification}");
        Console.WriteLine($"  material_change_assessment: {updated.MaterialChangeAssessment}");
        Console.WriteLine($"  technical_file_reference: {updated.TechnicalFileReference}");

        List<Tenant> tenants = Yaml.LoadTenants("tenants.yaml");
        DateOnly approvedOn = DateOnly.Parse(updated.Approval.ApprovalDate);
        Console.WriteLine($"\nRollout of 2.3.0 approved on {approvedOn:yyyy-MM-dd}");
        foreach (int offset in new[] { 0, 8, 31 })
        {
            DateOnly today = approvedOn.AddDays(offset);
            Console.WriteLine($"  day {offset} ({today:yyyy-MM-dd})");
            foreach (Tenant t in tenants)
                Console.WriteLine($"    {t.Id,-26} tier {t.RolloutTier}: {Eligible(t, approvedOn, today)}");
        }

        Console.WriteLine("\nPublish approved manifests as Foundry agent versions");
        List<ProjectsAgentVersion> published = [];
        foreach (Manifest m in new[] { old, updated })
        {
            published.Add(await Foundry.CreateAgent("m8h-life-governed", m.PromptText(), new()
            {
                ["manifest_version"] = m.Version,
                ["prompt_version"] = m.SystemPrompt.Version,
                ["prompt_hash"] = m.SystemPrompt.Hash[..16],
                ["approver"] = m.Approval.Approver,
            }));
        }

        await foreach (ProjectsAgentVersion v in Foundry.Project.AgentAdministrationClient.GetAgentVersionsAsync("m8h-life-governed"))
            Console.WriteLine($"  {v.Name} v{v.Version} metadata={{{string.Join(", ", v.Metadata.Select(kv => $"'{kv.Key}': '{kv.Value}'"))}}}");

        foreach (ProjectsAgentVersion agent in published) await Foundry.DeleteAgent(agent);
    }
}
