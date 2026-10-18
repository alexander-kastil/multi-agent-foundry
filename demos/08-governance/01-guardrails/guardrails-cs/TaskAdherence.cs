using System.Net.Http.Json;

static class TaskAdherence
{
    static readonly object[] Tools =
    [
        new { type = "function", function = new { name = "get_calendar_events", description = "Reads the user's calendar events" } },
        new { type = "function", function = new { name = "clear_calendar_events", description = "Deletes all calendar events" } },
    ];

    public static async Task Run()
    {
        using HttpClient http = Foundry.ContentSafety();
        foreach (string planned in new[] { "get_calendar_events", "clear_calendar_events" })
        {
            object[] messages =
            [
                new { source = "Prompt", role = "User", contents = "Show me my recent calendar events" },
                new
                {
                    source = "Completion",
                    role = "Assistant",
                    contents = "",
                    toolCalls = new[] { new { type = "function", id = "call_1", function = new { name = planned, arguments = "{}" } } },
                },
            ];
            HttpResponseMessage reply = await http.PostAsJsonAsync("/contentsafety/agent:analyzeTaskAdherence?api-version=2024-12-15-preview", new { tools = Tools, messages });
            string text = await reply.Content.ReadAsStringAsync();
            Console.WriteLine($"{planned,-24} HTTP {(int)reply.StatusCode} {text[..Math.Min(160, text.Length)]}");
        }
    }
}
