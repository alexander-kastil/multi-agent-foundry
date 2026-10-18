using System.Text.Json.Nodes;

public static class ServerCompaction
{
    const int Threshold = 6000;

    public static async Task Run()
    {
        var conversation = History.Flatten(History.BuildTurns(8));
        JsonNode response = null!;

        for (int day = 9; day <= 14; day++)
        {
            conversation.AddRange(History.Turn(day).Take(3));
            if (day == 14)
            {
                conversation.Add(Raw.User(History.Question));
            }

            response = await Raw.Create(new()
            {
                ["instructions"] = "Answer in one short sentence.",
                ["input"] = Raw.Items(conversation),
                ["store"] = false,
                ["max_output_tokens"] = 2000,
                ["context_management"] = new JsonArray(new JsonObject { ["type"] = "compaction", ["compact_threshold"] = Threshold }),
            });

            var output = response["output"]!.AsArray();
            conversation.AddRange(output.Select(item => item!.DeepClone()));
            int last = conversation.FindLastIndex(item => item["type"]?.GetValue<string>() == "compaction");
            if (last >= 0)
            {
                conversation = conversation.Skip(last).ToList();
            }

            string kinds = string.Join(", ", output.Select(item => $"'{item!["type"]}'"));
            Console.WriteLine($"day {day}: {Raw.Tokens(response, "input_tokens"),6} input tokens, output [{kinds}], window now {conversation.Count} items");
        }

        Console.WriteLine(Raw.Text(response));
    }
}
