record Secret(string Value, bool Enabled);

class InMemoryVault
{
    static readonly Dictionary<string, string[]> Roles = new()
    {
        ["style-checker"] = ["secrets/style-api-key"],
        ["compliance-checker"] = ["secrets/compliance-api-key"],
    };

    readonly Dictionary<string, Secret> secrets = [];

    public void SetSecret(string name, string value) => secrets[name] = new Secret(value, true);

    public void Disable(string name) => secrets[name] = secrets[name] with { Enabled = false };

    public Secret GetSecret(string name, string identity)
    {
        if (!Roles[identity].Contains($"secrets/{name[..name.LastIndexOf('-')]}"))
            throw new UnauthorizedAccessException($"{identity} has no Key Vault Secrets User assignment for {name}");
        return secrets[name];
    }
}

class KeyVaultSecretManager(InMemoryVault vault, string identity)
{
    public InMemoryVault Vault => vault;

    public string GetCurrentApiKey(string secretName)
    {
        Secret primary = vault.GetSecret($"{secretName}-primary", identity);
        return primary.Enabled ? primary.Value : vault.GetSecret($"{secretName}-secondary", identity).Value;
    }
}

static class AuthFlows
{
    static readonly (string Scenario, string Flow)[] Flows =
    [
        ("Agent calls Azure service (Cosmos DB, Foundry, Storage)", "managed identity"),
        ("Agent calls agent inside the same Azure trust boundary", "managed identity + workload identity"),
        ("Agent reads a user's Azure DevOps data", "on-behalf-of (OBO)"),
        ("Agent reads third-party SaaS data on a user's behalf", "OAuth2 authorization code + PKCE"),
        ("Agent calls a partner API that only accepts API keys", "key-based fallback with Key Vault"),
    ];

    public static void Run()
    {
        Console.WriteLine("Authentication flow selection");
        foreach ((string scenario, string flow) in Flows)
            Console.WriteLine($"  {scenario,-62} -> {flow}");

        InMemoryVault vault = new();
        vault.SetSecret("style-api-key-primary", "key-v1");
        vault.SetSecret("style-api-key-secondary", "key-v1");
        vault.SetSecret("compliance-api-key-primary", "compliance-key");

        KeyVaultSecretManager style = new(vault, "style-checker");
        Console.WriteLine("\nBlue-green rotation for style-checker");
        Console.WriteLine($"  before rotation: {style.GetCurrentApiKey("style-api-key")}");

        vault.SetSecret("style-api-key-secondary", "key-v2");
        vault.Disable("style-api-key-primary");
        Console.WriteLine($"  during rotation, primary disabled: {style.GetCurrentApiKey("style-api-key")}");

        vault.SetSecret("style-api-key-primary", "key-v2");
        Console.WriteLine($"  after rotation, primary restored: {style.GetCurrentApiKey("style-api-key")}");

        Console.WriteLine("\nSecret-level RBAC");
        try
        {
            style.Vault.GetSecret("compliance-api-key-primary", "style-checker");
        }
        catch (UnauthorizedAccessException e)
        {
            Console.WriteLine($"  {e.Message}");
        }
    }
}
