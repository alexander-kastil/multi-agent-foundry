using System.ComponentModel;
using Microsoft.Extensions.AI;

public static class Tools
{
    [Description("Lookup the weather for a given city name or zip code.")]
    private static WeatherReport LookupWeather(string? cityName, string? zipCode)
    {
        Console.WriteLine($"tool  : LookupWeather(cityName={cityName}, zipCode={zipCode})");
        return new(cityName, zipCode, "sunny", 21);
    }

    public static async Task Run(IChatClient inner)
    {
        IChatClient client = new ChatClientBuilder(inner)
            .UseFunctionInvocation()
            .Build();

        ChatOptions options = new()
        {
            Tools = [AIFunctionFactory.Create(LookupWeather)]
        };

        List<ChatMessage> messages =
        [
            new(ChatRole.System, "You are a weather chatbot."),
            new(ChatRole.User, "Is it sunny in Vienna?")
        ];

        ChatResponse response = await client.GetResponseAsync(messages, options);
        Console.WriteLine($"answer: {response.Text}");
    }

    public sealed record WeatherReport(string? CityName, string? ZipCode, string Weather, int TemperatureCelsius);
}
