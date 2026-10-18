using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Responses;

static class InjectionDefense
{
    public static async Task Run()
    {
        string model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
        string baseline = File.ReadAllText("prompts/v1.0.0.txt");
        string hardened = File.ReadAllText("prompts/v1.1.0.txt");
        JsonArray tests = JsonNode.Parse(File.ReadAllText("tests.json"))!.AsArray();

        AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
        ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForModel(model);

        Task<string> Ask(string instructions, string document) => Guards.FilteredCall(async () =>
        {
            ResponseResult result = await responses.CreateResponseAsync(new CreateResponseOptions
            {
                Instructions = instructions,
                InputItems = { ResponseItem.CreateUserMessageItem(document) },
            });
            return result.GetOutputText();
        });

        async Task<(string, List<string>)> LayerBaseline(string document) => (await Ask(baseline, document), []);

        async Task<(string, List<string>)> LayerStructural(string document) => (await Ask(hardened, Guards.WrapDocument(document)), []);

        async Task<(string, List<string>)> LayerFull(string document)
        {
            Scan scan = Guards.ScanForInjection(document);
            string instructions = scan.RiskLevel == "high" ? hardened + Guards.ElevatedSafetyNote : hardened;
            string output = await Ask(instructions, Guards.WrapDocument(document));
            return (output, Guards.ValidateOutput(output));
        }

        Dictionary<string, Func<string, Task<(string, List<string>)>>> layers = new()
        {
            ["1 no defense"] = LayerBaseline,
            ["2 delimiters + persona stability"] = LayerStructural,
            ["3 scan + delimiters + persona + output check"] = LayerFull,
        };

        foreach ((string name, var run) in layers)
        {
            Console.WriteLine($"\n=== {name} ===");
            int survived = 0;
            foreach (JsonNode? test in tests)
            {
                (string output, List<string> findings) = await run((string)test!["document"]!);
                bool ok = Guards.Passes(test, output);
                if (ok) survived++;
                string blocked = output == Guards.PlatformBlock ? " (blocked by platform jailbreak filter)" : "";
                Console.WriteLine($"  {test["id"],-28} {(ok ? "DEFENDED" : "COMPROMISED")}  output_flags=[{string.Join(", ", findings.Select(f => $"'{f}'"))}]{blocked}");
                string flat = string.Join(' ', output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                Console.WriteLine($"    {flat[..Math.Min(110, flat.Length)]}");
            }
            Console.WriteLine($"  defended {survived}/{tests.Count}");
        }
    }
}
