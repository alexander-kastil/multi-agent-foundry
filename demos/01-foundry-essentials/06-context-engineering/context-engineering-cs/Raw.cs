using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;

#pragma warning disable OPENAI001

public static class Raw
{
    public static async Task<JsonNode> Create(JsonObject body)
    {
        body["model"] = Session.Model;
        ClientResult result = await Session.Responses().CreateResponseAsync(Content(body), new RequestOptions());
        return JsonNode.Parse(result.GetRawResponse().Content)!;
    }

    public static async Task<JsonNode> Compact(JsonArray input)
    {
        JsonObject body = new() { ["model"] = Session.Model, ["input"] = input };
        ClientResult result = await Session.Responses().CompactResponseAsync("application/json", Content(body), new RequestOptions());
        return JsonNode.Parse(result.GetRawResponse().Content)!;
    }

    public static JsonArray Items(IEnumerable<JsonNode?> items) => new(items.Select(item => item!.DeepClone()).ToArray());

    public static JsonObject User(string text) => new() { ["role"] = "user", ["content"] = text };

    public static int Tokens(JsonNode response, string name) => response["usage"]![name]!.GetValue<int>();

    public static string Text(JsonNode response) =>
        string.Concat(response["output"]!.AsArray()
            .Where(item => item!["type"]!.GetValue<string>() == "message")
            .SelectMany(item => item!["content"]!.AsArray())
            .Select(part => part!["text"]!.GetValue<string>()));

    static BinaryContent Content(JsonObject body) => BinaryContent.Create(BinaryData.FromString(body.ToJsonString()));
}
