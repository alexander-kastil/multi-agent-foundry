using System.Text.Json.Nodes;

public static class Compact
{
    static string Kinds(IEnumerable<JsonNode?> items) =>
        string.Join(", ", items.GroupBy(item => item!["type"]?.GetValue<string>() ?? "message").Select(group => $"{group.Key}: {group.Count()}"));

    public static async Task Run()
    {
        var history = History.Flatten(History.BuildTurns(20));
        var compacted = await Raw.Compact(Raw.Items(history));

        Console.WriteLine($"items before : {{{Kinds(history)}}}");
        Console.WriteLine($"items after  : {{{Kinds(compacted["output"]!.AsArray())}}}");

        var input = Raw.Items(compacted["output"]!.AsArray());
        input.Add(Raw.User(History.Question));
        var response = await Raw.Create(new()
        {
            ["instructions"] = "Answer in one short sentence. If you do not know, say so.",
            ["input"] = input,
            ["store"] = false,
            ["max_output_tokens"] = 2000,
        });
        Console.WriteLine($"compact call : {Raw.Tokens(compacted, "input_tokens")} in, {Raw.Tokens(compacted, "output_tokens")} out");
        Console.WriteLine($"next turn    : {Raw.Tokens(response, "input_tokens")} input tokens");
        Console.WriteLine($"answer       : {Raw.Text(response)}");
    }
}
