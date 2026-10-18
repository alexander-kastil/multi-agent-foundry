using System.Text.Json;
using Microsoft.Extensions.Options;

namespace McpAuth.Auth;

public class ScopeChallengeMiddleware(RequestDelegate next, IOptions<EntraOptions> options)
{
    private static readonly HashSet<string> WriteTools = ["add_employee", "remove_employee"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.StartsWithSegments("/mcp"))
        {
            context.Request.EnableBuffering();
            var tool = await ReadToolNameAsync(context.Request);
            context.Request.Body.Position = 0;

            if (tool is not null && WriteTools.Contains(tool) && !HasScope(context.User, options.Value.WriteScope))
            {
                var metadata = $"{context.Request.Scheme}://{context.Request.Host}/.well-known/oauth-protected-resource/mcp";
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.Headers.WWWAuthenticate =
                    $"Bearer error=\"insufficient_scope\", scope=\"api://{options.Value.ClientId}/{options.Value.WriteScope}\", resource_metadata=\"{metadata}\", error_description=\"The {tool} tool needs the {options.Value.WriteScope} scope\"";
                return;
            }
        }

        await next(context);
    }

    private static bool HasScope(System.Security.Claims.ClaimsPrincipal user, string scope) =>
        (user.FindFirst("scp")?.Value ?? "").Split(' ').Contains(scope);

    private static async Task<string?> ReadToolNameAsync(HttpRequest request)
    {
        using var document = await JsonDocument.ParseAsync(request.Body);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("method", out var method) && method.GetString() == "tools/call"
            && root.TryGetProperty("params", out var parameters) && parameters.TryGetProperty("name", out var name))
        {
            return name.GetString();
        }
        return null;
    }
}
