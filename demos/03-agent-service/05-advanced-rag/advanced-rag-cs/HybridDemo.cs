using System.Globalization;

static class Labeled
{
    public static readonly Dictionary<string, (string Query, string Expected)[]> Pairs = new()
    {
        ["drug_formulary"] =
        [
            ("00087-6060-05", "f2"),
            ("Zestril", "f3"),
            ("Coumadin aspirin interaction", "f4"),
            ("strong CYP3A4 inhibitor", "f7"),
            ("Lantus", "f6"),
            ("NDC 00003-0894-21", "f10"),
        ],
        ["clinical_guidelines"] =
        [
            ("heart attack treatment time window", "g1"),
            ("blood thinner for irregular heartbeat", "g4"),
            ("high blood sugar emergency potassium", "g6"),
            ("underactive thyroid treatment", "g8"),
            ("clot in the lung assessment", "g9"),
            ("clot-busting drug for brain attack", "g10"),
        ],
    };

    public const string RerankQuery = "What should I monitor after starting an ACE inhibitor in a patient with diabetes?";
}

static class HybridDemo
{
    static readonly (string Source, string Query)[] SceneQueries =
    [
        ("clinical_guidelines", "heart attack"),
        ("drug_formulary", "NDC 00071-0156-23"),
        ("drug_formulary", "atorvastatin contraindications"),
        ("clinical_guidelines", "breathlessness with pain on breathing in"),
    ];

    static readonly double[] Weights = [0.3, 0.7, 1.0, 1.5];
    static readonly Dictionary<string, double> SourceWeights = new() { ["drug_formulary"] = 0.3, ["clinical_guidelines"] = 1.5 };

    const string RerankPrompt = """
        Rate how well this document answers the query on a scale of 1-10.

        Query: {0}

        Document Title: {1}
        Document Content: {2}

        Provide only a number between 1-10, where 10 means the document directly and completely answers the query.
        """;

    static async Task<double> ReciprocalRank(LocalIndex index, string query, string expected, string mode, double vectorWeight = 1.0)
    {
        List<string> ids = (await index.Search(query, mode, vectorWeight, top: 10)).Select(hit => hit.Id).ToList();
        int position = ids.IndexOf(expected);
        return position >= 0 ? 1.0 / (position + 1) : 0.0;
    }

    static async Task<double> MeanReciprocalRank((string Query, string Expected)[] pairs, LocalIndex index, string mode, double vectorWeight = 1.0)
    {
        double total = 0;
        foreach (var (query, expected) in pairs)
            total += await ReciprocalRank(index, query, expected, mode, vectorWeight);
        return total / pairs.Length;
    }

    public static async Task Run()
    {
        Dictionary<string, LocalIndex> indexes = new()
        {
            ["drug_formulary"] = await LocalIndex.Create("drug_formulary"),
            ["clinical_guidelines"] = await LocalIndex.Create("clinical_guidelines"),
        };

        Console.WriteLine("=== 1. Keyword vs vector vs hybrid (hybrid uses the per-source weights tuned in scene 2) ===");
        foreach (var (source, query) in SceneQueries)
        {
            Console.WriteLine($"\n[{source}] {query}");
            foreach (string mode in new[] { "keyword", "vector", "hybrid" })
            {
                List<Hit> hits = await indexes[source].Search(query, mode, SourceWeights[source], top: 3);
                Console.WriteLine($"  {mode,-8} {(hits.Count > 0 ? string.Join(" | ", hits.Select(hit => hit.Title)) : "no results")}");
            }
        }

        Console.WriteLine("\n=== 2. Tune the vector weight per knowledge source (MRR over labeled queries) ===");
        Console.WriteLine($"{"source",-22}" + string.Concat(Weights.Select(w => $"w={w.ToString("0.0", CultureInfo.InvariantCulture),-6}")) + "keyword  vector");
        foreach (var (source, pairs) in Labeled.Pairs)
        {
            string row = "";
            foreach (double w in Weights)
                row += $"{await MeanReciprocalRank(pairs, indexes[source], "hybrid", w),-8:0.00}";
            double keyword = await MeanReciprocalRank(pairs, indexes[source], "keyword");
            double vector = await MeanReciprocalRank(pairs, indexes[source], "vector");
            Console.WriteLine($"{source,-22}{row}{keyword,-9:0.00}{vector:0.00}");
        }

        Console.WriteLine("\n=== 3. LLM-as-reranker on the hybrid candidates ===");
        Console.WriteLine($"Query: {Labeled.RerankQuery}");
        List<Hit> candidates = [];
        foreach (LocalIndex index in indexes.Values)
            candidates.AddRange(await index.Search(Labeled.RerankQuery, "hybrid", top: 4));
        candidates = candidates.OrderByDescending(hit => hit.Score).ToList();
        Console.WriteLine("hybrid order : " + string.Join(" | ", candidates.Take(5).Select(hit => hit.Title)));
        List<(Hit Hit, double Score)> rated = [];
        foreach (Hit candidate in candidates.Take(5))
        {
            string prompt = string.Format(RerankPrompt, Labeled.RerankQuery, candidate.Title, candidate.Content);
            string answer = (await Models.Responses.CreateResponseAsync(prompt)).Value.GetOutputText();
            rated.Add((candidate, double.Parse(answer.Trim(), CultureInfo.InvariantCulture)));
        }
        Console.WriteLine("reranked top3: " + string.Join(" | ", rated.OrderByDescending(pair => pair.Score).Take(3).Select(pair => $"{pair.Hit.Title} ({pair.Score:0})")));
    }
}
