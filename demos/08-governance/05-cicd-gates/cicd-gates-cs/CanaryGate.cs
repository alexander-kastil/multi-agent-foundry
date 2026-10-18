using System.Text.Json.Nodes;
using Azure.AI.Projects.Agents;

static class CanaryGate
{
    const string Name = "m8h-cicd-scanner";

    const string Stable = """
        You are a security scanner for source code.
        Reply with a single JSON object and nothing else: {"vulnerable": true|false, "violations": [{"line": int, "rule": str, "severity": "error"|"warning"|"info"}]}.
        Set vulnerable to true only when the code contains an exploitable security flaw.
        """;

    const string CanaryGood = """
        You are a security scanner for source code.
        Output exactly one JSON object and no other text: {"vulnerable": true|false, "violations": [{"line": int, "rule": str, "severity": "error"|"warning"|"info"}]}.
        Mark vulnerable as true when the code contains an exploitable security flaw such as injection, hardcoded credentials or unsafe evaluation.
        """;

    const string CanaryRegressed = """
        You are a friendly code reviewer who focuses on style and readability.
        Reply in one short prose sentence. Do not use JSON. Security is handled by another team, so do not mention vulnerabilities.
        """;

    const double Threshold = 0.05;

    static async Task<Dictionary<string, double>> Evaluate(ProjectsAgentVersion agent, List<JsonNode> cases)
    {
        int correct = 0, valid = 0;
        foreach (JsonNode caseItem in cases)
        {
            string text = (await Foundry.Ask(agent.Name, agent.Version, (string)caseItem["code"]!)).Trim();
            if (text.StartsWith('{') && text.EndsWith('}'))
            {
                valid++;
                if ((bool)JsonNode.Parse(text)!["vulnerable"]! == (bool)caseItem["vulnerable"]!) correct++;
            }
        }
        return new() { ["accuracy"] = (double)correct / cases.Count, ["schema_valid_rate"] = (double)valid / cases.Count };
    }

    static string Show(Dictionary<string, double> metrics) =>
        "{" + string.Join(", ", metrics.Select(m => $"'{m.Key}': {(m.Value == Math.Floor(m.Value) ? m.Value.ToString("F1") : m.Value.ToString("R"))}")) + "}";

    public static async Task<int> Run(bool regress)
    {
        List<JsonNode> cases = File.ReadAllLines("eval-set.jsonl").Select(l => JsonNode.Parse(l)!).ToList();
        string variant = regress ? CanaryRegressed : CanaryGood;

        ProjectsAgentVersion stable = await Foundry.CreateAgent(Name, Stable, new() { ["role"] = "stable" });
        await Foundry.PinEndpoint(Name, stable.Version);
        ProjectsAgentVersion canary = await Foundry.CreateAgent(Name, variant, new() { ["role"] = "canary" });
        Console.WriteLine($"Stable v{stable.Version}, canary v{canary.Version}, endpoint serves v{await Foundry.ServedVersion(Name)}");

        var baseline = await Evaluate(stable, cases);
        var candidate = await Evaluate(canary, cases);
        Console.WriteLine($"baseline: {Show(baseline)}");
        Console.WriteLine($"canary:   {Show(candidate)}");

        bool passed = baseline.Keys.All(m => baseline[m] - candidate[m] <= Threshold);
        if (passed)
        {
            await Foundry.PinEndpoint(Name, canary.Version);
            Console.WriteLine($"Quality gate PASS (threshold {Threshold * 100:0}%): endpoint promoted to v{await Foundry.ServedVersion(Name)}");
        }
        else
        {
            Console.WriteLine($"Quality gate FAIL (threshold {Threshold * 100:0}%): endpoint stays on v{await Foundry.ServedVersion(Name)}");
        }

        await Foundry.DeleteAgent(Name, canary.Version);
        await Foundry.DeleteAgent(Name, stable.Version);
        return passed ? 0 : 1;
    }
}
