using System.Text.Json;

static class RoutingDemo
{
    static readonly string[] Sources = ["drug_formulary", "clinical_guidelines", "lab_references"];
    static readonly Dictionary<string, double> SourceWeights = new() { ["drug_formulary"] = 0.3, ["clinical_guidelines"] = 1.5, ["lab_references"] = 1.0 };
    const double High = 0.7;
    const double Low = 0.4;

    static readonly string[] Queries =
    [
        "What are side effects of metformin?",
        "What is the first-line treatment for type 2 diabetes?",
        "What TSH level indicates hypothyroidism?",
        "How do I manage a patient with diabetic ketoacidosis who is hypokalaemic?",
        "Tell me about the Northwind cafeteria opening hours.",
    ];

    const string ClassificationPrompt = """
        Analyze this clinical query and give, for each knowledge source, the probability (0 to 1) that it is needed to answer it.

        Query: {0}

        Knowledge sources:
        - drug_formulary: medication information including brand and generic names, dosing, side effects, contraindications, interactions
        - clinical_guidelines: diagnosis criteria, treatment algorithms, clinical protocols, disease management
        - lab_references: laboratory test reference ranges, interpretation guidelines

        Output only a JSON object: {{"drug_formulary": 0.0, "clinical_guidelines": 0.0, "lab_references": 0.0}}
        """;

    static async Task<Dictionary<string, double>> Classify(string query)
    {
        string text = (await Models.Responses.CreateResponseAsync(string.Format(ClassificationPrompt, query))).Value.GetOutputText().Trim();
        text = text.Replace("```json", "").Replace("```", "").Trim();
        return JsonSerializer.Deserialize<Dictionary<string, double>>(text)!;
    }

    static List<string> ChooseSources(Dictionary<string, double> probabilities, ISet<string> unavailable)
    {
        List<string> ranked = probabilities.Keys.OrderByDescending(source => probabilities[source]).ToList();
        List<string> chosen = ranked.Where(source => probabilities[source] > High).ToList();
        if (chosen.Count == 0)
            chosen = probabilities.Values.Max() > Low ? ranked.Where(source => probabilities[source] > Low).ToList() : ranked;
        List<string> available = chosen.Where(source => !unavailable.Contains(source)).ToList();
        return available.Count > 0 ? available : Sources.Where(source => !unavailable.Contains(source)).ToList();
    }

    static List<(Hit Hit, double Norm)> Normalize(List<Hit> hits)
    {
        double low = hits.Min(hit => hit.Score);
        double high = hits.Max(hit => hit.Score);
        return hits.Select(hit => (hit, high == low ? 1.0 : (hit.Score - low) / (high - low))).ToList();
    }

    public static async Task Run()
    {
        Dictionary<string, LocalIndex> indexes = [];
        foreach (string source in Sources)
            indexes[source] = await LocalIndex.Create(source);

        async Task<(Dictionary<string, double> Probabilities, List<string> Chosen, List<(Hit Hit, double Norm)> Merged)> RoutedSearch(string query, ISet<string> unavailable)
        {
            Dictionary<string, double> probabilities = await Classify(query);
            List<string> chosen = ChooseSources(probabilities, unavailable);
            List<Hit>[] results = await Task.WhenAll(chosen.Select(source => indexes[source].Search(query, "hybrid", SourceWeights[source], top: 3)));
            return (probabilities, chosen, results.SelectMany(Normalize).OrderByDescending(pair => pair.Norm).ToList());
        }

        string Describe(List<(Hit Hit, double Norm)> merged) =>
            string.Join(" | ", merged.Take(3).Select(pair => $"{pair.Hit.Title} [{pair.Hit.Source}]"));

        int totalSources = 0;
        foreach (string query in Queries)
        {
            var (probabilities, chosen, merged) = await RoutedSearch(query, new HashSet<string>());
            totalSources += chosen.Count;
            Console.WriteLine($"\nQuery: {query}");
            Console.WriteLine("  confidence: {" + string.Join(", ", probabilities.Select(pair => $"'{pair.Key}': {Math.Round(pair.Value, 2)}")) + "}");
            Console.WriteLine("  searched  : [" + string.Join(", ", chosen.Select(source => $"'{source}'")) + "]");
            Console.WriteLine($"  top results: {Describe(merged)}");
        }
        Console.WriteLine($"\naverage sources searched per query: {(double)totalSources / Queries.Length:0.0} of {Sources.Length}");

        string fallbackQuery = "What are side effects of metformin?";
        Console.WriteLine($"\nQuery with drug_formulary unavailable: {fallbackQuery}");
        var fallback = await RoutedSearch(fallbackQuery, new HashSet<string> { "drug_formulary" });
        Console.WriteLine("  searched  : [" + string.Join(", ", fallback.Chosen.Select(source => $"'{source}'")) + "] (fallback because the classified source is down)");
        Console.WriteLine($"  top results: {Describe(fallback.Merged)}");
    }
}
