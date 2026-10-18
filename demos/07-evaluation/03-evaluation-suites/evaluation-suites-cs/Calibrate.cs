using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

static class Calibrate
{
    const int PassScore = 7;

    static double CohenKappa(bool[] a, bool[] b)
    {
        int total = a.Length;
        double observed = a.Zip(b).Count(p => p.First == p.Second) / (double)total;
        double expected = new[] { true, false }.Sum(v => a.Count(x => x == v) / (double)total * (b.Count(x => x == v) / (double)total));
        return expected == 1 ? 1.0 : (observed - expected) / (1 - expected);
    }

    public static async Task<int> Run()
    {
        JsonArray examples = JsonNode.Parse(File.ReadAllText("calibration_set.json"))!.AsArray();
        IChatClient judge = Foundry.ChatClient(Foundry.Model);

        int[] scores = new int[examples.Count];
        await Parallel.ForEachAsync(Enumerable.Range(0, examples.Count), new ParallelOptions { MaxDegreeOfParallelism = 4 }, async (index, token) =>
        {
            JsonNode item = examples[index]!;
            scores[index] = (await TaskCompletionEvaluator.Judge(judge, (string)item["customer_request"]!, (string)item["response"]!, (string)item["ground_truth"]!)).Score;
        });

        bool[] human = examples.Select(e => (int)e!["human_score"]! >= PassScore).ToArray();
        bool[] automatic = scores.Select(s => s >= PassScore).ToArray();
        double kappa = CohenKappa(human, automatic);
        double difference = scores.Zip(examples, (s, e) => Math.Abs(s - (int)e!["human_score"]!)).Average();
        Console.WriteLine($"Judge {Foundry.Model}: Cohen's kappa {kappa:F2} on {examples.Count} labeled examples");
        Console.WriteLine($"Mean absolute score difference {difference:F1}");
        Console.WriteLine(kappa > 0.7 ? "Reliable for gating" : "Revise the rubric before gating");

        string[] judges = Foundry.JudgeModels;
        Dictionary<string, IChatClient> clients = judges.ToDictionary(m => m, Foundry.ChatClient);
        List<(double Consensus, int Spread, string Individual)> consensus = [];
        foreach (JsonNode? item in examples)
        {
            int[] individual = await Task.WhenAll(judges.Select(async model =>
                (await TaskCompletionEvaluator.Judge(clients[model], (string)item!["customer_request"]!, (string)item["response"]!, (string)item["ground_truth"]!)).Score));
            consensus.Add((individual.Average(), individual.Max() - individual.Min(),
                "{" + string.Join(", ", judges.Zip(individual, (m, s) => $"'{m}': {s}")) + "}"));
        }

        List<int> flagged = consensus.Select((c, i) => (c, i)).Where(x => x.c.Spread > 3).Select(x => x.i).ToList();
        Console.WriteLine($"Consensus across [{string.Join(", ", judges.Select(j => $"'{j}'"))}]: {flagged.Count} of {consensus.Count} examples need human review [{string.Join(", ", flagged)}]");
        for (int i = 0; i < consensus.Count; i++)
            Console.WriteLine($"  example {i}: consensus {consensus[i].Consensus:F1} spread {consensus[i].Spread} {consensus[i].Individual}");
        return 0;
    }
}
