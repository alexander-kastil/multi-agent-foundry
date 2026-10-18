namespace McpAuth.Auth;

public class EntraOptions
{
    public const string SectionName = "Entra";

    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string RequiredScope { get; set; } = "hr.read";
    public string WriteScope { get; set; } = "hr.write";

    public string Authority => $"https://login.microsoftonline.com/{TenantId}/v2.0";
}
