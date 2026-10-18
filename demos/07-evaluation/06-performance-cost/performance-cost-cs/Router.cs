using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenAI.Responses;

record RouteOutcome(string Response, int AssignedTier, int TierUsed, int QualityScore, bool NeedsHumanReview, double Cost, double LatencyS);

class ModelRouter
{
    static readonly Dictionary<int, (double Input, double Output)> IllustrativePricePerMillionTokens = new()
    {
        [1] = (0.25, 2.0),
        [2] = (2.5, 15.0),
        [3] = (1.25, 10.0),
    };

    static readonly string[] EscalationKeywords = ["exception", "legal", "complaint", "lawsuit", "refund policy"];

    readonly Dictionary<int, string> tierDeployments = new()
    {
        [1] = Foundry.Model("TIER1_MODEL"),
        [2] = Foundry.Model("TIER2_MODEL"),
        [3] = Foundry.Model("TIER3_MODEL"),
    };

    readonly Dictionary<int, int> qualityFloors = new() { [1] = 75, [2] = 85, [3] = 60 };

    public int ClassifyComplexity(string request, double transactionAmount = 0)
    {
        string lowered = request.ToLowerInvariant();
        if (EscalationKeywords.Any(lowered.Contains) || transactionAmount > 500) return 3;
        if (request.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < 12 && Regex.IsMatch(request, @"\d")) return 1;
        return 2;
    }

    public int EvaluateQuality(string response, int tier)
    {
        int score = 100;
        string lowered = response.ToLowerInvariant();
        int words = response.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (new[] { "i'm not sure", "i don't know", "unclear", "cannot determine" }.Any(lowered.Contains)) score -= 30;
        if (words < 8) score -= 30;
        if (tier == 1 && words > 150) score -= 10;
        return Math.Max(score, 0);
    }

    public double EstimateCost(int tier, ResponseResult response)
    {
        (double input, double output) = IllustrativePricePerMillionTokens[tier];
        return (response.Usage.InputTokenCount * input + response.Usage.OutputTokenCount * output) / 1_000_000;
    }

    public async Task<RouteOutcome> RouteWithQualityFloor(string request, int maxTier = 3, int? forceTier = null)
    {
        int assignedTier = forceTier ?? ClassifyComplexity(request);
        int tier = assignedTier;
        double totalCost = 0;
        long started = Stopwatch.GetTimestamp();
        while (true)
        {
            ResponseResult response = await Foundry.Respond(
                tierDeployments[tier],
                request,
                "You are a concise customer service assistant for a chocolate shop. Answer in at most three sentences.");
            totalCost += EstimateCost(tier, response);
            string text = response.GetOutputText();
            int quality = EvaluateQuality(text, tier);
            if (quality >= qualityFloors[tier] || tier == maxTier)
                return new RouteOutcome(text, assignedTier, tier, quality, quality < qualityFloors[tier], totalCost, Stopwatch.GetElapsedTime(started).TotalSeconds);
            tier++;
        }
    }
}
