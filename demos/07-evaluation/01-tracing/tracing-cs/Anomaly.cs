record AnomalyEvent(string Agent, string Metric, int Time);

record Incident(string Agent, int Start, List<AnomalyEvent> Anomalies);

record Baseline(double Mean, double Std);

record Detection(bool IsAnomaly, double ZScore, string Severity);

class TokenUsageAnomalyDetector(int baselineDays = 14, double sigmaThreshold = 3.0)
{
    readonly Dictionary<string, Baseline> baselines = [];

    static double PopulationStdDev(IEnumerable<double> values)
    {
        double mean = values.Average();
        return Math.Sqrt(values.Average(v => (v - mean) * (v - mean)));
    }

    public void UpdateBaseline(string agentId, double[] dailyTokenCounts)
    {
        if (dailyTokenCounts.Length < baselineDays) return;
        double[] recent = dailyTokenCounts[^baselineDays..];
        baselines[agentId] = new Baseline(recent.Average(), PopulationStdDev(recent));
    }

    public Detection Detect(string agentId, double currentTokenCount)
    {
        Baseline baseline = baselines[agentId];
        double z = baseline.Std == 0 ? 0 : (currentTokenCount - baseline.Mean) / baseline.Std;
        return new Detection(Math.Abs(z) > sigmaThreshold, z, Math.Abs(z) > 3.0 ? "critical" : "warning");
    }
}

class RoutingDriftDetector(double sigmaThreshold = 3.0)
{
    public Detection Detect(double[] historicalShares, double currentShare)
    {
        double mean = historicalShares.Average();
        double std = Math.Sqrt(historicalShares.Average(v => (v - mean) * (v - mean)));
        double z = std == 0 ? 0 : (currentShare - mean) / std;
        return new Detection(Math.Abs(z) > sigmaThreshold, z, "warning");
    }
}

static class Anomaly
{
    static List<Incident> Correlate(IEnumerable<AnomalyEvent> anomalies, int windowSeconds, Dictionary<string, string[]> dependencyChain)
    {
        List<Incident> incidents = [];
        foreach (AnomalyEvent anomaly in anomalies.OrderBy(a => a.Time))
        {
            Incident? match = incidents.FirstOrDefault(i =>
                new[] { i.Agent }.Concat(dependencyChain.GetValueOrDefault(i.Agent) ?? []).Contains(anomaly.Agent) && anomaly.Time - i.Start <= windowSeconds);
            if (match is null) incidents.Add(new Incident(anomaly.Agent, anomaly.Time, [anomaly]));
            else match.Anomalies.Add(anomaly);
        }
        return incidents;
    }

    public static void Run()
    {
        TokenUsageAnomalyDetector detector = new();
        detector.UpdateBaseline("product-search-agent", [850, 920, 880, 910, 870, 900, 890, 880, 920, 870, 910, 900, 880, 890]);
        foreach (int usage in new[] { 905, 2650 })
        {
            Detection result = detector.Detect("product-search-agent", usage);
            Console.WriteLine($"tokens={usage} z={result.ZScore:F2} anomaly={result.IsAnomaly} severity={result.Severity}");
        }

        Detection drift = new RoutingDriftDetector().Detect([0.78, 0.75, 0.80, 0.72, 0.77, 0.74, 0.79], 0.45);
        Console.WriteLine($"routing share=0.45 z={drift.ZScore:F2} anomaly={drift.IsAnomaly}");

        AnomalyEvent[] anomalies =
        [
            new("payment-agent", "latency", 0),
            new("payment-agent", "error_rate", 60),
            new("orchestrator-agent", "error_rate", 90),
            new("payment-agent", "tokens", 120),
        ];
        List<Incident> incidents = Correlate(anomalies, 300, new() { ["payment-agent"] = ["orchestrator-agent"] });
        Console.WriteLine($"{anomalies.Length} anomalies grouped into {incidents.Count} incident(s)");
    }
}
