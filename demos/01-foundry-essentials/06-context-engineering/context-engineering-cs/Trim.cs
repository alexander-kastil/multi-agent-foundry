public static class Trim
{
    public static async Task Run()
    {
        var turns = History.BuildTurns(20);
        var variants = new[] { ("all 20 turns", turns), ("last 4 turns", turns.TakeLast(4).ToList()) };

        foreach (var (name, kept) in variants)
        {
            var input = Raw.Items(History.Flatten(kept));
            input.Add(Raw.User(History.Question));
            var response = await Raw.Create(new()
            {
                ["instructions"] = "Answer in one short sentence. If you do not know, say so.",
                ["input"] = input,
                ["store"] = false,
                ["max_output_tokens"] = 2000,
            });
            Console.WriteLine($"{name,-13}: {Raw.Tokens(response, "input_tokens"),6} input tokens");
            Console.WriteLine($"{"",-13}  {Raw.Text(response)}");
        }
    }
}
