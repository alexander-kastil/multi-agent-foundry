using System.Text.RegularExpressions;

static class ChunkingDemo
{
    static readonly string[] Queries =
    [
        "potassium is below 3.3, do I hold insulin",
        "what infusion rate of insulin in DKA",
    ];
    const int FixedSize = 300;

    static List<string> FixedChunks(string text, int size) =>
        Enumerable.Range(0, (text.Length + size - 1) / size).Select(i => text.Substring(i * size, Math.Min(size, text.Length - i * size))).ToList();

    static List<(string Heading, string Content)> StructuralChunks(string text) =>
        Regex.Split(text, @"\n(?=## )").Skip(1).Select(section =>
        {
            string[] lines = section.Split('\n');
            return (lines[0].TrimStart('#', ' '), string.Join("\n", lines.Skip(1)).Trim());
        }).ToList();

    static async Task<(int Best, double Score)> TopHit(string query, List<string> chunks)
    {
        float[][] vectors = (await Models.Embed(chunks)).Select(Models.Normalize).ToArray();
        float[] q = Models.Normalize((await Models.Embed([query]))[0]);
        double[] scores = vectors.Select(vector => Models.Dot(vector, q)).ToArray();
        int best = Array.IndexOf(scores, scores.Max());
        return (best, scores[best]);
    }

    static string Flat(string text) => text.Replace("\n", " ");

    public static async Task Run()
    {
        string text = File.ReadAllText("data/dka_protocol.md").Replace("\r\n", "\n");
        string title = text.Split('\n')[0].TrimStart('#', ' ');
        List<string> fixedChunks = FixedChunks(text, FixedSize);
        var sections = StructuralChunks(text);
        List<string> sectionTexts = sections.Select(section => $"{section.Heading}. {section.Content}").ToList();
        string overview = sections[0].Content;

        Console.WriteLine($"Document: {title} ({text.Length} characters)");
        Console.WriteLine($"fixed-size chunks: {fixedChunks.Count} of {FixedSize} characters; structural chunks: {sections.Count} sections");

        foreach (string query in Queries)
        {
            Console.WriteLine($"\nQuery: {query}");
            var (best, score) = await TopHit(query, fixedChunks);
            Console.WriteLine($"\n  fixed-size  (score {score:0.000}), cuts across section boundaries:");
            string flat = Flat(fixedChunks[best]);
            Console.WriteLine("    " + flat[..Math.Min(300, flat.Length)]);
            (best, score) = await TopHit(query, sectionTexts);
            Console.WriteLine($"\n  structural  (score {score:0.000}), one complete section:");
            Console.WriteLine("    " + Flat(sections[best].Content));
            Console.WriteLine($"\n  parent-child adds the parent context for '{title}':");
            Console.WriteLine("    " + Flat(overview)[..160] + " ...");
        }
    }
}
