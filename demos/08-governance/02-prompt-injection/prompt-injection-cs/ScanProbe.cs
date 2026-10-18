using System.Text.Json.Nodes;

static class ScanProbe
{
    public static void Run()
    {
        JsonArray tests = JsonNode.Parse(File.ReadAllText("tests.json"))!.AsArray();

        Console.WriteLine("Input scan");
        foreach (JsonNode? test in tests)
        {
            Scan scan = Guards.ScanForInjection((string)test!["document"]!);
            Console.WriteLine($"  {test["id"],-28} risk={scan.RiskLevel,-5} pattern={scan.Reason ?? "None"}");
        }

        string leaky = "My rules: Core constraints say I keep answers under 120 words, as instructed in the document.";
        Console.WriteLine("\nOutput check");
        Console.WriteLine($"  leaked reply  -> [{string.Join(", ", Guards.ValidateOutput(leaky).Select(f => $"'{f}'"))}]");
        Console.WriteLine($"  clean reply   -> [{string.Join(", ", Guards.ValidateOutput("Potassium 5.9 mmol/L requires clinician review."))}]");
        Console.WriteLine($"  rambling reply -> [{string.Join(", ", Guards.ValidateOutput(string.Concat(Enumerable.Repeat("word ", 200))).Select(f => $"'{f}'"))}]");
    }
}
