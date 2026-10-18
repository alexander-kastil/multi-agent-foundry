using System.Diagnostics;

static class Quorum
{
    static readonly string[] SpokeNames = ["market_analysis", "risk_assessment", "esg_evaluation"];

    public static async Task<object> ParallelResearch(string query, Dictionary<string, TimeSpan> timeouts, int required = 2)
    {
        Stopwatch watch = Stopwatch.StartNew();
        SpokeOutcome[] results = await Task.WhenAll(SpokeNames.Select(name => Resilience.InvokeSpoke(name, query, timeouts[name])));
        List<string> successes = results.Where(r => r.Status == "success").Select(r => r.Spoke).Order().ToList();
        double elapsed = Math.Round(watch.Elapsed.TotalSeconds, 1);
        if (successes.Count < required)
            return new { status = "insufficient_quorum", successful = successes.Count, required, elapsed_sec = elapsed };
        return new { status = "success", completed = successes, missing = SpokeNames.Where(name => !successes.Contains(name)).ToList(), elapsed_sec = elapsed };
    }

    public static async Task TripCircuit(string spokeName)
    {
        for (int attempt = 1; attempt <= 4; attempt++)
        {
            SpokeOutcome outcome = await Resilience.InvokeSpoke(spokeName, "Review MSFT for the growth portfolio.", TimeSpan.FromMilliseconds(10));
            Console.WriteLine($"Call {attempt}: {outcome.Status}, failures={Resilience.Circuits[spokeName].Failures}");
        }
    }
}
