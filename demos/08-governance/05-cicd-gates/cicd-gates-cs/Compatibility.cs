using System.Text;
using System.Text.Json.Nodes;
using YamlDotNet.Serialization;

static class Compatibility
{
    static Dictionary<string, List<string>> LoadGraph(string path)
    {
        IDeserializer yaml = new DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return yaml.Deserialize<ManifestFile>(File.ReadAllText(path)).Agents.ToDictionary(a => a.Name, a => a.DependsOn);
    }

    class ManifestFile
    {
        public List<ManifestEntry> Agents { get; set; } = [];
    }

    class ManifestEntry
    {
        public string Name { get; set; } = "";
        public List<string> DependsOn { get; set; } = [];
    }

    static List<List<string>> DeploymentWaves(Dictionary<string, List<string>> graph)
    {
        List<List<string>> waves = [];
        HashSet<string> done = [];
        while (done.Count < graph.Count)
        {
            List<string> wave = graph.Where(n => !done.Contains(n.Key) && n.Value.All(done.Contains)).Select(n => n.Key).Order().ToList();
            waves.Add(wave);
            done.UnionWith(wave);
        }
        return waves;
    }

    static HashSet<string> DependentsOf(Dictionary<string, List<string>> graph, string agent)
    {
        HashSet<string> direct = graph.Where(n => n.Value.Contains(agent)).Select(n => n.Key).ToHashSet();
        HashSet<string> result = [.. direct];
        foreach (string d in direct) result.UnionWith(DependentsOf(graph, d));
        return result;
    }

    static string[] Strings(JsonNode? node) => node!.AsArray().Select(n => (string)n!).ToArray();

    static List<(string Agent, string Reason)> BreakingChanges(JsonObject baseline, JsonObject current)
    {
        List<(string, string)> found = [];
        foreach ((string agent, JsonNode? old) in baseline)
        {
            JsonNode? updated = current[agent];
            if (updated is null)
            {
                found.Add((agent, "tool removed"));
                continue;
            }
            HashSet<string> known = [.. Strings(old!["required_params"]), .. Strings(old["optional_params"])];
            foreach (string p in Strings(updated["required_params"]).Except(Strings(old["required_params"])).Order())
                found.Add((agent, known.Contains(p) ? $"optional parameter '{p}' became required" : $"new required parameter '{p}'"));
            foreach (string f in Strings(old["response_fields"]).Except(Strings(updated["response_fields"])).Order())
                found.Add((agent, $"response field '{f}' removed"));
        }
        return found;
    }

    static string RenderWorkflow(Dictionary<string, List<string>> graph, List<List<string>> waves)
    {
        StringBuilder text = new();
        text.AppendLine("name: Deploy Multi-Agent System");
        text.AppendLine("on:");
        text.AppendLine("  workflow_dispatch:");
        text.AppendLine("    inputs:");
        text.AppendLine("      environment:");
        text.AppendLine("        type: choice");
        text.AppendLine("        options: [staging, production]");
        text.AppendLine("        required: true");
        text.AppendLine("jobs:");
        foreach (string name in waves.SelectMany(w => w))
        {
            text.AppendLine($"  deploy-{name}:");
            if (graph[name].Count > 0) text.AppendLine($"    needs: [{string.Join(", ", graph[name].Select(d => $"deploy-{d}"))}]");
            text.AppendLine("    runs-on: ubuntu-latest");
            text.AppendLine("    steps:");
            text.AppendLine($"      - name: Deploy {name} agent");
            text.AppendLine("        run: |");
            text.AppendLine("          az containerapp update \\");
            text.AppendLine($"            --name {name}-agent \\");
            text.AppendLine("            --resource-group fabrikam-${{ inputs.environment }} \\");
            text.AppendLine($"            --image fabrikam.azurecr.io/{name}:${{{{ github.sha }}}}");
        }
        return text.ToString();
    }

    public static int Run()
    {
        Dictionary<string, List<string>> graph = LoadGraph("agent-dependencies.yml");
        List<List<string>> waves = DeploymentWaves(graph);

        Console.WriteLine("Deployment waves (agents in one wave deploy in parallel)");
        for (int i = 0; i < waves.Count; i++) Console.WriteLine($"  wave {i + 1}: {string.Join(", ", waves[i])}");

        JsonObject baseline = JsonNode.Parse(File.ReadAllText("contracts/baseline.json"))!.AsObject();
        JsonObject current = JsonNode.Parse(File.ReadAllText("contracts/current.json"))!.AsObject();
        var changes = BreakingChanges(baseline, current);

        Console.WriteLine("\nContract validation against baseline");
        foreach ((string agent, string reason) in changes)
        {
            Console.WriteLine($"  BREAKING {agent}: {reason}");
            Console.WriteLine($"           affected dependents: {string.Join(", ", DependentsOf(graph, agent).Order())}");
        }

        Directory.CreateDirectory("out");
        File.WriteAllText("out/deploy-agents.yml", RenderWorkflow(graph, waves));
        Console.WriteLine("\nGenerated out/deploy-agents.yml from the dependency manifest");

        Console.WriteLine($"\nGate result: {(changes.Count > 0 ? "FAIL, breaking changes block deployment" : "PASS")}");
        return changes.Count > 0 ? 1 : 0;
    }
}
