using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

record Summary(double TaskCompletionRate, double AvgGoalAchievement, Dictionary<string, double> ByInteractionType, int CaseCount);

static class SuiteRunner
{
    const int PassScore = 7;

    public static async Task<Summary> Run(string version, IEnumerable<JsonNode> suite, string label)
    {
        List<JsonNode> cases = suite.ToList();
        IChatClient judge = Foundry.ChatClient(Foundry.Model);
        (int Score, string Reasoning)[] verdicts = new (int, string)[cases.Count];
        string[] responses = new string[cases.Count];

        await Parallel.ForEachAsync(Enumerable.Range(0, cases.Count), new ParallelOptions { MaxDegreeOfParallelism = 6 }, async (index, token) =>
        {
            JsonNode item = cases[index];
            responses[index] = await Foundry.AskAgent(version, (string)item["customer_request"]!);
            verdicts[index] = await TaskCompletionEvaluator.Judge(judge, (string)item["customer_request"]!, responses[index], item["ground_truth"]!.ToJsonString());
        });

        Directory.CreateDirectory("results");
        JsonArray rows = [];
        for (int i = 0; i < cases.Count; i++)
        {
            rows.Add(new JsonObject
            {
                ["case_id"] = cases[i]["id"]!.ToString(),
                ["interaction_type"] = cases[i]["interaction_type"]!.ToString(),
                ["customer_request"] = cases[i]["customer_request"]!.ToString(),
                ["response"] = responses[i],
                ["task_completion.score"] = verdicts[i].Score,
                ["task_completion.reasoning"] = verdicts[i].Reasoning,
            });
        }
        File.WriteAllText($"results/{label}_result.json", rows.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return new Summary(
            verdicts.Count(v => v.Score >= PassScore) / (double)cases.Count,
            verdicts.Average(v => v.Score),
            cases.Select((c, i) => (Type: c["interaction_type"]!.ToString(), Passed: verdicts[i].Score >= PassScore))
                .GroupBy(x => x.Type).ToDictionary(g => g.Key, g => g.Count(x => x.Passed) / (double)g.Count()),
            cases.Count);
    }
}
