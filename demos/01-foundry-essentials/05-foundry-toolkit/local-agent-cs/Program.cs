using System.ComponentModel;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using DotNetEnv;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Extensions.AI;

Env.Load();

if (args.FirstOrDefault() == "ask")
{
    await Ask.Run(args[1]);
    return;
}

[Description("Get the current weather for a city.")]
static string GetWeather([Description("The city to look up.")] string city) =>
    $"The weather in {city} is sunny with a high of 21 degrees Celsius.";

AIAgent agent = new AIProjectClient(
        new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
        new DefaultAzureCredential())
    .AsAIAgent(
        model: Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!,
        instructions: "You are a weather assistant. Always use the get_weather tool and keep answers to one sentence.",
        name: "weather-assistant",
        tools: [AIFunctionFactory.Create(GetWeather, "get_weather")]);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:8088");
builder.Services.AddFoundryResponses(agent);

var app = builder.Build();
app.MapFoundryResponses();
app.Run();

public static class Ask
{
    public static async Task Run(string question)
    {
        using HttpClient http = new() { BaseAddress = new Uri("http://localhost:8088") };
        var reply = await http.PostAsJsonAsync("/responses", new
        {
            model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME"),
            input = question,
        });
        using var json = JsonDocument.Parse(await reply.Content.ReadAsStringAsync());
        var root = json.RootElement;

        var items = root.GetProperty("output").EnumerateArray().ToList();
        var answer = items.Last().GetProperty("content")[0].GetProperty("text");

        Console.WriteLine($"status : {root.GetProperty("status")}");
        Console.WriteLine($"items  : [{string.Join(", ", items.Select(item => $"'{item.GetProperty("type")}'"))}]");
        Console.WriteLine($"answer : {answer}");
    }
}
