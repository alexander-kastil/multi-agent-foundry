using System.Text.Json.Nodes;
using Microsoft.Agents.AI;

static class Supervisor
{
    const string CheckerInstructions =
        """You review research reports. Reply as JSON only, without code fences: {"verdict": "APPROVE" or "REVISE", "issues": ["..."]}. """ +
        "Require a recommendation, a risk statement and a compliance statement.";

    const string MakerInstructions = "You rewrite research reports to fix the listed issues and keep them under 200 words.";

    public static async Task<(bool Approved, int Attempts)> Supervise(string report, int maxRetries = 1)
    {
        AIAgent checker = Spokes.MakeAgent("report_checker", CheckerInstructions);
        AIAgent maker = Spokes.MakeAgent("report_maker", MakerInstructions);
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            JsonNode verdict = JsonNode.Parse((await checker.RunAsync(report)).Text)!;
            string label = verdict["verdict"]!.GetValue<string>();
            Console.WriteLine($"Supervisor attempt {attempt + 1}: {label} {verdict["issues"]!.ToJsonString()}");
            if (label == "APPROVE")
                return (true, attempt + 1);
            if (attempt < maxRetries)
                report = (await maker.RunAsync($"Report:\n{report}\n\nIssues:\n{verdict["issues"]!.ToJsonString()}")).Text;
        }
        return (false, maxRetries + 1);
    }
}
