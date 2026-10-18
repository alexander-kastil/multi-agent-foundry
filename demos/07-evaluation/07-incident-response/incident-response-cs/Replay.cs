using System.Text.Json;

record ReplayResult(string Output, bool Valid, bool DivergesFromProduction);

class SnapshotStore
{
    const string Directory = "replay-snapshots";
    static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public SnapshotStore() => System.IO.Directory.CreateDirectory(Directory);

    public void Save(Snapshot snapshot) =>
        File.WriteAllText(Path.Combine(Directory, $"{snapshot.TraceId}.json"), JsonSerializer.Serialize(snapshot, Options));

    public Snapshot Load(string traceId) =>
        JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(Path.Combine(Directory, $"{traceId}.json")))!;
}

static class AgentReplayFramework
{
    public static List<string> VerifyPromptHashes(Snapshot snapshot, Dictionary<string, string> currentPrompts)
    {
        List<string> mismatches = [];
        if (!currentPrompts.TryGetValue("pricing-agent", out string? current))
            mismatches.Add("pricing-agent: prompt not found in current deployment");
        else if (Pipeline.Sha256(current) != snapshot.PromptHash)
            mismatches.Add("pricing-agent: hash mismatch, prompt changed since the production failure");
        return mismatches;
    }

    public static async Task<ReplayResult> ReplayPricingStep(Snapshot snapshot, string? promptOverride = null)
    {
        string output = await Foundry.AskModel(snapshot.DeploymentName, promptOverride ?? snapshot.PromptContent, Pipeline.RequestText(snapshot.MockOrder));
        return new ReplayResult(output, Pipeline.PricePattern().IsMatch(output.Trim()), output.Trim() != snapshot.ProductionOutput.Trim());
    }

    public static async Task<(Dictionary<string, bool> Results, string? FirstFailingVersion)> BisectPromptVersions(Snapshot snapshot, Dictionary<string, string> promptVersions)
    {
        Dictionary<string, bool> results = [];
        foreach ((string label, string prompt) in promptVersions)
            results[label] = (await ReplayPricingStep(snapshot, prompt)).Valid;
        return (results, results.FirstOrDefault(pair => !pair.Value).Key);
    }
}
