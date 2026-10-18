using System.Text.Json.Nodes;

public static class Caching
{
    public static async Task Run()
    {
        string run = Guid.NewGuid().ToString("N")[..8];
        string handbook = $"Travel desk handbook {run}\n" + string.Join("\n", Enumerable.Range(1, 4).Select(day => $"Day {day}\n{History.Forecast(day)}"));
        string[] questions = ["Which day has the most rain?", "Which day is the windiest?", "Which day is the warmest?"];

        async Task Ask(string label, string instructions, string question)
        {
            var response = await Raw.Create(new()
            {
                ["instructions"] = instructions,
                ["input"] = new JsonArray(Raw.User(question)),
                ["max_output_tokens"] = 2000,
                ["prompt_cache_key"] = $"m1-handbook-{run}",
                ["prompt_cache_retention"] = "24h",
                ["store"] = false,
            });
            int cached = response["usage"]!["input_tokens_details"]!["cached_tokens"]!.GetValue<int>();
            Console.WriteLine($"{label,-22}: {Raw.Tokens(response, "input_tokens"),5} input, {cached,5} cached");
        }

        for (int number = 1; number <= questions.Length; number++)
        {
            await Ask($"stable prefix, call {number}", $"{handbook}\nAnswer in one short sentence.", questions[number - 1]);
        }

        await Ask("variable text first", $"Question: {questions[0]}\n{handbook}\nAnswer in one short sentence.", questions[0]);
    }
}
