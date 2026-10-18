using System.Text.Json;
using System.Text.Json.Nodes;

static class RegressionGate
{
    record Check(double Delta, double Limit, bool Pass);

    static Dictionary<string, Check> Compare(Summary baseline, Summary current, JsonNode thresholds)
    {
        Dictionary<string, (double Delta, double Limit)> checks = new()
        {
            ["task_completion"] = (current.TaskCompletionRate - baseline.TaskCompletionRate, -(double)thresholds["task_completion_drop"]!),
            ["goal_achievement"] = (current.AvgGoalAchievement - baseline.AvgGoalAchievement, -(double)thresholds["goal_achievement_drop"]!),
        };
        foreach ((string name, double rate) in current.ByInteractionType)
            checks[$"type:{name}"] = (rate - baseline.ByInteractionType.GetValueOrDefault(name, rate), -(double)thresholds["per_type_drop"]!);
        return checks.ToDictionary(c => c.Key, c => new Check(c.Value.Delta, c.Value.Limit, c.Value.Delta >= c.Value.Limit));
    }

    static string Pct(double value) => $"{value * 100:0}%";

    static Summary LoadBaseline()
    {
        JsonNode node = JsonNode.Parse(File.ReadAllText("baseline.json"))!;
        return new Summary(
            (double)node["task_completion_rate"]!,
            (double)node["avg_goal_achievement"]!,
            node["by_interaction_type"]!.AsObject().ToDictionary(p => p.Key, p => (double)p.Value!),
            (int)node["case_count"]!);
    }

    public static async Task<int> Run(string[] args)
    {
        bool saveBaseline = args.Contains("--save-baseline");
        int candidateIndex = Array.IndexOf(args, "--candidate");
        string candidate = candidateIndex >= 0 ? args[candidateIndex + 1] : "v2";

        List<JsonNode> suite = JsonNode.Parse(File.ReadAllText("synthetic_test_suite.json"))!.AsArray().Select(n => n!).ToList();
        List<JsonNode> probes = JsonNode.Parse(File.ReadAllText("historical_probes.json"))!.AsArray().Select(n => n!).ToList();
        JsonNode thresholds = JsonNode.Parse(File.ReadAllText("thresholds.json"))!;

        Dictionary<string, string> versions = await Foundry.DeployVersions();
        string label = saveBaseline ? "v1" : candidate;
        string version = versions[label];
        List<JsonNode> canaries = suite.Where(c => (bool)c["canary"]!).ToList();
        Summary canaryResult = await SuiteRunner.Run(version, canaries, $"{label}-canaries");
        Console.WriteLine($"Canaries ({label}): {Pct(canaryResult.TaskCompletionRate)} completion");
        if (canaryResult.TaskCompletionRate < (double)thresholds["canary_min_rate"]! && !saveBaseline)
        {
            await Foundry.DeleteVersions(versions);
            Console.WriteLine("BLOCK - canary failure");
            return 1;
        }
        Summary current = await SuiteRunner.Run(version, suite.Concat(probes), $"{label}-full");
        await Foundry.DeleteVersions(versions);

        if (saveBaseline)
        {
            JsonObject saved = new()
            {
                ["task_completion_rate"] = current.TaskCompletionRate,
                ["avg_goal_achievement"] = current.AvgGoalAchievement,
                ["by_interaction_type"] = JsonSerializer.SerializeToNode(current.ByInteractionType),
                ["case_count"] = current.CaseCount,
            };
            File.WriteAllText("baseline.json", saved.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Baseline saved: {Pct(current.TaskCompletionRate)} completion, {current.AvgGoalAchievement:F1} avg score");
            return 0;
        }

        Summary baseline = LoadBaseline();
        Dictionary<string, Check> comparison = Compare(baseline, current, thresholds);
        Console.WriteLine("REGRESSION TEST RESULTS");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine($"Task completion  baseline {Pct(baseline.TaskCompletionRate)}  current {Pct(current.TaskCompletionRate)}");
        Console.WriteLine($"Goal achievement baseline {baseline.AvgGoalAchievement:F1}  current {current.AvgGoalAchievement:F1}");
        foreach ((string name, Check result) in comparison)
            Console.WriteLine($"{name,-32} delta {result.Delta:+0.00;-0.00;+0.00} limit {result.Limit:+0.00;-0.00;+0.00} {(result.Pass ? "PASS" : "FAIL")}");

        if (comparison.Values.All(r => r.Pass))
        {
            Console.WriteLine("APPROVE DEPLOYMENT");
            return 0;
        }
        Console.WriteLine("BLOCK - quality regression detected");
        return 1;
    }
}
