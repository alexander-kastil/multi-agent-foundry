using System.Diagnostics;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using OpenAI.Responses;

#pragma warning disable OPENAI001

public static class Compare
{
    private static readonly Dictionary<string, string> Prompts = new()
    {
        ["lookup"] = "What is the capital of Austria?",
        ["code"] = "Write a Python function that merges overlapping intervals and explain its time complexity.",
        ["reasoning"] = "A bat and a ball cost 1.10 in total and the bat costs 1.00 more than the ball. Solve it with algebra and explain why intuition fails.",
    };

    public static async Task Run(IEnumerable<string> deployments)
    {
        AIProjectClient project = new(
            new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
            new DefaultAzureCredential());

        Console.WriteLine($"{"deployment",-22} {"prompt",-10} {"served by",-32} {"sec",5} {"tokens",7}");
        foreach (var deployment in deployments)
        {
            ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForModel(deployment);
            foreach (var (label, prompt) in Prompts)
            {
                var started = Stopwatch.GetTimestamp();
                ResponseResult response = await responses.CreateResponseAsync(prompt);
                var elapsed = Stopwatch.GetElapsedTime(started).TotalSeconds;
                Console.WriteLine($"{deployment,-22} {label,-10} {response.Model,-32} {elapsed,5:F1} {response.Usage.TotalTokenCount,7}");
            }
        }
    }
}
