using System.Text.Json.Nodes;
using Microsoft.Agents.AI;

static class PromptChain
{
    static readonly Dictionary<string, string> Branches = new()
    {
        ["GROWTH_DRIVEN"] = "Produce a five year discounted cash flow summary with growth projections, terminal value assumption and intrinsic value estimate.",
        ["MATURE_EARNINGS"] = "Produce a comparable multiples valuation using P/E and EV/EBITDA versus peers.",
        ["ASSET_HEAVY"] = "Produce a book value based valuation.",
    };

    static JsonNode ValidateMetrics(JsonNode metrics)
    {
        double margin = metrics["operating_margin"]!.GetValue<double>();
        if (margin < -1.0 || margin > 1.0 || metrics["debt_to_equity"]!.GetValue<double>() < 0)
            throw new InvalidOperationException($"Metrics out of range: {metrics.ToJsonString()}");
        return metrics;
    }

    public static async Task Run()
    {
        AIAgent extractor = Agents.Make("metric_extractor", "Extract financial metrics from filing text. Reply with raw JSON only.");
        JsonNode metrics = ValidateMetrics(await Agents.AskJson(
            extractor,
            $"Return JSON with revenue_millions, revenue_growth_3yr, operating_margin, debt_to_equity from: {Registry.FilingText}"));
        Console.WriteLine($"Step 1 metrics: {metrics.ToJsonString()}");

        AIAgent classifier = Agents.Make("profile_classifier", "Classify company financial profiles. Reply with raw JSON only.");
        JsonNode profile = await Agents.AskJson(
            classifier,
            $"Classify as GROWTH_DRIVEN, MATURE_EARNINGS or ASSET_HEAVY. Return JSON with classification, confidence, reasoning. Metrics: {metrics.ToJsonString()}");
        string classification = profile["classification"]!.GetValue<string>();
        Console.WriteLine($"Step 2 profile: {classification}");

        AIAgent valuer = Agents.Make("valuation_analyst", "You value companies. Reply with raw JSON only.");
        JsonNode valuation = await Agents.AskJson(
            valuer,
            $"{Branches[classification]} Reply as flat JSON with two string fields: method and valuation_summary (under 60 words). Metrics: {metrics.ToJsonString()}");
        Console.WriteLine($"Step 3 valuation method: {valuation["method"]}");

        AIAgent commentator = Agents.Make("analyst_commentator", "You write concise analyst commentary.");
        string commentary = (await commentator.RunAsync($"Write two sentences of commentary on: {valuation.ToJsonString()}")).Text;
        Console.WriteLine($"Step 4 commentary: {commentary}");
    }
}
