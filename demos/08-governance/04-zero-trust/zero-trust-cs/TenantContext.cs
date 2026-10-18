using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

static class TenantContext
{
    const string ExpectedAudience = "api://fabrikam-code-review";
    const string ExpectedIssuer = "https://login.microsoftonline.com/fabrikam-tenant/v2.0";

    static readonly AsyncLocal<string?> tenant = new();

    public static string? CurrentTenant
    {
        get => tenant.Value;
        set => tenant.Value = value;
    }

    public static RsaSecurityKey NewSigningKey() => new(RSA.Create(2048));

    public static string IssueToken(RsaSecurityKey privateKey, string tenantId) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Audience = ExpectedAudience,
            Issuer = ExpectedIssuer,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { ["tenant_id"] = tenantId },
            SigningCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256),
        });

    public static async Task<string> ExtractTenantFromToken(string authorizationHeader, RsaSecurityKey publicKey)
    {
        string token = authorizationHeader.Split("Bearer ")[1];
        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidAudience = ExpectedAudience,
            ValidIssuer = ExpectedIssuer,
            IssuerSigningKey = publicKey,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
        });
        if (!result.IsValid) throw result.Exception;
        return (string)result.Claims["tenant_id"];
    }
}

class PartitionedStore
{
    public Dictionary<string, Dictionary<string, Review>> Partitions { get; } = [];

    public void CreateItem(Review document, string partitionKey)
    {
        if (!Partitions.TryGetValue(partitionKey, out var partition)) Partitions[partitionKey] = partition = [];
        partition[document.Id] = document;
    }

    public Review ReadItem(string item, string partitionKey) => Partitions[partitionKey][item];

    public List<Review> QueryItems(string partitionKey) =>
        Partitions.TryGetValue(partitionKey, out var partition) ? [.. partition.Values] : [];
}

record Review(string Id, string TenantId, string CodeFile, string[] Findings);

class CodeReviewRepository(PartitionedStore store)
{
    public void SaveReview(string reviewId, string codeFile, string[] findings)
    {
        string tenantId = TenantContext.CurrentTenant!;
        store.CreateItem(new Review(reviewId, tenantId, codeFile, findings), tenantId);
    }

    public Review? GetReview(string reviewId)
    {
        string tenantId = TenantContext.CurrentTenant!;
        if (!(store.Partitions.GetValueOrDefault(tenantId)?.ContainsKey(reviewId) ?? false)) return null;
        return store.ReadItem(reviewId, tenantId);
    }

    public List<Review> ListReviews() => store.QueryItems(TenantContext.CurrentTenant!);
}
