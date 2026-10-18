using System.Text.Json.Nodes;

static class AuditReport
{
    static double StdDev(IEnumerable<double> values)
    {
        double mean = values.Average();
        return Math.Sqrt(values.Average(v => (v - mean) * (v - mean)));
    }

    public static void Run()
    {
        List<JsonNode> entries = File.ReadAllLines("out/audit.jsonl").Select(l => JsonNode.Parse(l)!).ToList();

        Console.WriteLine("Agent version timeline");
        var timeline = entries.GroupBy(e => ((string)e["agent_id"]!, (string)e["model_version"]!, (string)e["prompt_version"]!));
        foreach (var group in timeline)
        {
            var stamps = group.Select(e => (string)e["timestamp"]!).Order().ToList();
            Console.WriteLine($"  {group.Key.Item1} model={group.Key.Item2} prompt={group.Key.Item3} first={stamps.First()} last={stamps.Last()} invocations={stamps.Count}");
        }

        Console.WriteLine("\nQuality scores by team context");
        foreach (var team in entries.GroupBy(e => (string)e["output"]!["team_context"]!).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            List<double> scores = team.Select(e => (double)e["output"]!["quality_score"]!).ToList();
            Console.WriteLine($"  {team.Key,-11} avg={scores.Average():F1} stdev={StdDev(scores):F1} submissions={scores.Count}");
        }

        Console.WriteLine("\nDecision trail for one correlation id");
        string target = (string)entries[0]["correlation_id"]!;
        foreach (JsonNode e in entries.Where(e => (string)e["correlation_id"]! == target))
            Console.WriteLine($"  {e["timestamp"]} {e["action_type"]} agent={e["agent_id"]} input_hash={((string)e["input_hash"]!)[..16]} output={e["output"]!.ToJsonString()}");
    }
}
