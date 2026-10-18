using System.Text.RegularExpressions;
using Microsoft.ML.Tokenizers;

record Turn(string Role, string Content);

class ContextAssembler
{
    static readonly string[] ImportantPhrases = ["i prefer", "please don't", "make sure to", "business account", "corporate"];
    static readonly string[] CommitmentPhrases = ["i'll", "we will", "expect"];
    readonly Tokenizer tokenizer = TiktokenTokenizer.CreateForEncoding("o200k_base");

    public int CountTokens(string text) => tokenizer.CountTokens(text);

    Dictionary<string, string?> BuildDigest(List<Turn> history)
    {
        Dictionary<string, string?> digest = new()
        {
            ["customer_identified"] = null,
            ["customer_tier"] = null,
            ["order_number"] = null,
            ["order_status"] = null,
            ["return_eligible"] = null,
        };
        foreach (Turn turn in history)
        {
            string lowered = turn.Content.ToLowerInvariant();
            Match match = Regex.Match(turn.Content, @"order #?(\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                digest["order_number"] = match.Groups[1].Value;
                digest["customer_identified"] = "True";
            }
            if (lowered.Contains("gold member")) digest["customer_tier"] = "gold";
            if (lowered.Contains("return") && lowered.Contains("not eligible")) digest["return_eligible"] = "False";
            if (lowered.Contains("delivered") && turn.Role == "assistant") digest["order_status"] = "delivered";
        }
        return digest;
    }

    List<Turn> FilterImportantTurns(IEnumerable<Turn> history) =>
        history.Where(turn =>
        {
            string content = turn.Content.ToLowerInvariant();
            return ImportantPhrases.Any(content.Contains) || (turn.Role == "assistant" && CommitmentPhrases.Any(content.Contains));
        }).ToList();

    static string CompressToolResults(Dictionary<string, Dictionary<string, object>> toolResults)
    {
        List<string> parts = [];
        foreach ((string name, Dictionary<string, object> result) in toolResults)
        {
            if (name == "product_lookup")
                parts.Add($"{name}: name={result["product_name"]}, price={result["current_price"]}, stock={result["stock_level"]}");
            else if (name == "order_lookup")
                parts.Add($"{name}: order_id={result["order_id"]}, status={result["fulfillment_status"]}, items={((List<string>)result["items"]).Count}");
        }
        return string.Join("; ", parts);
    }

    public (int Used, List<string> Pruned) AssembleContext(List<Turn> history, string systemPrompt, Dictionary<string, Dictionary<string, object>> toolResults, int tokenBudget, int windowTurns = 3)
    {
        Dictionary<string, string?> digest = BuildDigest(history);
        List<Turn> window = history.TakeLast(windowTurns * 2).Select(turn => turn with { }).ToList();
        List<Turn> important = FilterImportantTurns(history.Take(history.Count - windowTurns * 2));
        List<string> system = [systemPrompt];
        List<string> digestLines = ["Established facts: " + string.Join(", ", digest.Where(pair => pair.Value is not null).Select(pair => $"{pair.Key}={pair.Value}"))];
        List<string> tools = toolResults.Count > 0 ? [$"Tool results: {CompressToolResults(toolResults)}"] : [];
        List<string> pruned = [];

        int Total() => system.Concat(digestLines).Concat(tools).Concat(important.Select(t => t.Content)).Concat(window.Select(t => t.Content)).Sum(CountTokens);

        if (Total() > tokenBudget)
        {
            for (int i = 0; i < window.Count; i++)
                window[i] = window[i] with { Content = window[i].Content[..Math.Min(100, window[i].Content.Length)] + "..." };
            pruned.Add("window summarized");
        }
        if (Total() > tokenBudget)
        {
            important.Clear();
            pruned.Add("important turns dropped");
        }
        if (Total() > tokenBudget)
        {
            tools.Clear();
            pruned.Add("tool results dropped");
        }
        return (Total(), pruned);
    }
}
