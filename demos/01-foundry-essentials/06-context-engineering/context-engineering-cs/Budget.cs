public static class Budget
{
    public static async Task Run()
    {
        foreach (int days in new[] { 2, 10, 20 })
        {
            var input = Raw.Items(History.Flatten(History.BuildTurns(days)));
            input.Add(Raw.User(History.Question));
            var response = await Raw.Create(new()
            {
                ["instructions"] = "Answer in one short sentence.",
                ["input"] = input,
                ["store"] = false,
                ["max_output_tokens"] = 2000,
            });
            Console.WriteLine($"{days,3} days : {Raw.Tokens(response, "input_tokens"),6} input tokens, {Raw.Tokens(response, "output_tokens"),3} output tokens");
            Console.WriteLine($"         {Raw.Text(response)}");
        }
    }
}
