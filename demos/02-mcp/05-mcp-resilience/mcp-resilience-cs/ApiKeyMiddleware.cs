using System.Security.Cryptography;
using System.Text;

namespace McpResilience;

public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var expected = Encoding.UTF8.GetBytes(configuration["MCP_API_KEY"] ?? "");
        var supplied = Encoding.UTF8.GetBytes(context.Request.Headers["x-api-key"].ToString());
        var lengthsMatch = supplied.Length == expected.Length;
        var bytesMatch = CryptographicOperations.FixedTimeEquals(Pad(supplied, expected.Length), expected);

        if (!(lengthsMatch & bytesMatch))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    private static byte[] Pad(byte[] value, int length)
    {
        var padded = new byte[length];
        Array.Copy(value, padded, Math.Min(value.Length, length));
        return padded;
    }
}
