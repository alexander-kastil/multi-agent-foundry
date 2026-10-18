using System.Text.Json.Nodes;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;

static class Foundry
{
    public static readonly DefaultAzureCredential Credential = new();

    public static string Model => Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
    public static string PolicyId => Environment.GetEnvironmentVariable("RAI_POLICY_ID")!;
    public static Uri Endpoint => new(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);

    public static AIProjectClient Project() => new(Endpoint, Credential);

    public static HttpClient ContentSafety()
    {
        string token = Credential.GetToken(new TokenRequestContext(["https://cognitiveservices.azure.com/.default"])).Token;
        HttpClient http = new() { BaseAddress = new Uri($"{Endpoint.Scheme}://{Endpoint.Host}") };
        http.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return http;
    }

    public static string Flagged(JsonNode entry, bool includeDetected = false)
    {
        var hits = entry["content_filter_results"]!.AsObject()
            .Where(r => r.Value is JsonObject o && ((bool?)o["filtered"] == true || (includeDetected && (bool?)o["detected"] == true)))
            .Select(r => $"\"{r.Key}\": {r.Value!.ToJsonString()}");
        return hits.Any() ? "{" + string.Join(", ", hits) + "}" : "nothing flagged";
    }
}
