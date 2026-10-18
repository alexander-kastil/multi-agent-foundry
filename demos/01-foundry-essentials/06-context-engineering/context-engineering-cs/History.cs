using System.Text.Json.Nodes;

public static class History
{
    public const string Question = "What is my booking reference and who am I travelling with?";

    static readonly string[] Cities = ["Salzburg", "Graz", "Linz", "Innsbruck", "Vienna", "Klagenfurt", "Bregenz", "Eisenstadt"];
    static readonly string[] Skies = ["clear", "light rain", "cloudy", "sunny spells", "showers", "fog"];

    public static string Forecast(int day) => string.Join("\n", Enumerable.Range(0, 24).Select(hour =>
        $"{hour:00}:00 {Skies[(hour + day) % Skies.Length]}, {8 + hour * day % 14} C, wind {5 + (hour + day) % 20} km/h, rain {hour * day % 60} percent"));

    public static List<JsonNode> Turn(int day)
    {
        string city = Cities[day % Cities.Length];
        string first = day == 1 ? "My booking reference is ZX-4417 and I am travelling with my sister Marta. " : "";
        return
        [
            Raw.User($"{first}Day {day}: what is the weather in {city}?"),
            new JsonObject
            {
                ["type"] = "function_call",
                ["call_id"] = $"call_{day}",
                ["name"] = "get_forecast",
                ["arguments"] = new JsonObject { ["city"] = city, ["day"] = day }.ToJsonString(),
                ["status"] = "completed",
            },
            new JsonObject { ["type"] = "function_call_output", ["call_id"] = $"call_{day}", ["output"] = Forecast(day) },
            new JsonObject
            {
                ["type"] = "message",
                ["role"] = "assistant",
                ["status"] = "completed",
                ["content"] = new JsonArray(new JsonObject
                {
                    ["type"] = "output_text",
                    ["text"] = $"Day {day} in {city} is mostly {Skies[day % Skies.Length]}, so plan indoor stops for the afternoon.",
                }),
            },
        ];
    }

    public static List<List<JsonNode>> BuildTurns(int days) => Enumerable.Range(1, days).Select(Turn).ToList();

    public static List<JsonNode> Flatten(IEnumerable<List<JsonNode>> turns) => turns.SelectMany(turn => turn).ToList();
}
