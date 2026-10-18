using System.Diagnostics;

record SegmentProfile(int[] DefaultTiers, int HistoryTurns, double CacheThreshold, double P95LatencyS, double CostLimit);

static class Tradeoffs
{
    public static readonly Dictionary<string, SegmentProfile> SegmentProfiles = new()
    {
        ["gold"] = new([2, 3], 5, 0.93, 3, 0.015),
        ["standard"] = new([1, 2], 3, 0.90, 6, 0.005),
        ["bulk_api"] = new([1], 0, 0.85, 12, 0.001),
    };

    public static (bool LatencyOk, bool CostOk) MeetsSlo(string segment, double p95LatencyS, double averageCost)
    {
        SegmentProfile profile = SegmentProfiles[segment];
        return (p95LatencyS <= profile.P95LatencyS, averageCost <= profile.CostLimit);
    }

    public static async Task<double> TimedCalls(string[] prompts, bool parallel)
    {
        string model = Foundry.Model("TIER1_MODEL");
        long started = Stopwatch.GetTimestamp();
        if (parallel)
            await Task.WhenAll(prompts.Select(prompt => Foundry.Respond(model, prompt)));
        else
            foreach (string prompt in prompts) await Foundry.Respond(model, prompt);
        return Stopwatch.GetElapsedTime(started).TotalSeconds;
    }
}

class QualityCircuitBreaker(double ratingFloor = 3.8, int window = 20)
{
    readonly List<double> ratings = [];

    public bool Open { get; private set; }

    public bool Record(double rating)
    {
        ratings.Add(rating);
        List<double> recent = ratings.TakeLast(window).ToList();
        if (recent.Count >= window / 2 && recent.Average() < ratingFloor) Open = true;
        return Open;
    }

    public string Settings(string optimized) =>
        Open ? "{model_tier: 3, cache_enabled: False, history_turns: 5}" : optimized;
}
