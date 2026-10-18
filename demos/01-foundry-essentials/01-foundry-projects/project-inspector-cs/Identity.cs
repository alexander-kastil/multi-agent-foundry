using System.Text.Json;
using Azure.Core;
using Azure.Identity;

public static class Identity
{
    public static void Run()
    {
        var token = new DefaultAzureCredential()
            .GetToken(new TokenRequestContext(["https://ai.azure.com/.default"]))
            .Token;

        var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
        using var claims = JsonDocument.Parse(Convert.FromBase64String(payload));
        var root = claims.RootElement;

        string Claim(string name) => root.TryGetProperty(name, out var value) ? value.GetString()! : "";

        Console.WriteLine($"audience : {Claim("aud")}");
        Console.WriteLine($"identity : {(Claim("upn") != "" ? Claim("upn") : Claim("appid"))}");
        Console.WriteLine($"type     : {Claim("idtyp")}");
        Console.WriteLine($"tenant   : {Claim("tid")[..8]}...");
    }
}
