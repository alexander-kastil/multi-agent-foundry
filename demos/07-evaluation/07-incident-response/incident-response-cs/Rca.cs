record Request(double Time, string TraceId, bool Success, long PricingMs, long ToolMs, string PriceText);

record Change(double Time, string Type, string Agent, string From, string To);

record Hypothesis(string Name, bool Confirmed);

static class Rca
{
    public static Dictionary<string, string> Differential(List<Request> requests)
    {
        Dictionary<string, string> result = [];
        foreach ((string name, List<Request> items) in new[] { ("successful", requests.Where(r => r.Success).ToList()), ("failed", requests.Where(r => !r.Success).ToList()) })
        {
            long avg = items.Count > 0 ? (long)Math.Round(items.Average(item => item.PricingMs)) : 0;
            double rate = items.Count > 0 ? Math.Round(items.Count(item => item.PriceText.Contains('$')) / (double)items.Count, 2) : 0;
            result[name] = $"{{count: {items.Count}, avg_pricing_ms: {avg}, currency_symbol_rate: {rate}}}";
        }
        return result;
    }

    public static double? FirstFailureAfter(List<Request> requests, double changeTime) =>
        requests.Where(r => !r.Success && r.Time >= changeTime).Select(r => (double?)r.Time).Min();
}

class HypothesisFramework(List<Change> changeLog, List<Request> requests, double toolLatencyBaselineMs = 50)
{
    IEnumerable<Change> ChangesOf(string type) => changeLog.Where(change => change.Type == type);

    Hypothesis ModelDeployment() => new("model deployment change", ChangesOf("model_deployment").Any());

    Hypothesis PromptRegression()
    {
        Change? change = ChangesOf("prompt_deployment").LastOrDefault();
        if (change is null) return new("prompt regression", false);
        List<Request> before = requests.Where(r => r.Time < change.Time).ToList();
        List<Request> after = requests.Where(r => r.Time >= change.Time).ToList();
        double rateBefore = before.Count(r => !r.Success) / (double)Math.Max(before.Count, 1);
        double rateAfter = after.Count(r => !r.Success) / (double)Math.Max(after.Count, 1);
        return new("prompt regression", rateAfter - rateBefore > 0.2);
    }

    Hypothesis ToolFailure() => new("tool failure", requests.Average(r => r.ToolMs) > 3 * toolLatencyBaselineMs);

    Hypothesis OrchestrationLogic() => new("orchestration logic regression", ChangesOf("routing_change").Any());

    Hypothesis ConfigurationChange() => new("configuration change", ChangesOf("config_change").Any());

    public List<Hypothesis> Investigate()
    {
        List<Hypothesis> tested = [];
        foreach (Func<Hypothesis> check in new Func<Hypothesis>[] { ModelDeployment, PromptRegression, ToolFailure, OrchestrationLogic, ConfigurationChange })
        {
            Hypothesis result = check();
            tested.Add(result);
            if (result.Confirmed) break;
        }
        return tested;
    }
}
