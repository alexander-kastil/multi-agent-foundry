using McpAuth.Auth;
using McpAuth.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;
using ModelContextProtocol.Authentication;

var builder = WebApplication.CreateBuilder(args);
var entra = builder.Configuration.GetSection(EntraOptions.SectionName).Get<EntraOptions>()!;
builder.Services.Configure<EntraOptions>(builder.Configuration.GetSection(EntraOptions.SectionName));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = entra.Authority;
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidAudience = entra.ClientId,
        ValidIssuer = entra.Authority,
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateLifetime = true
    };
})
.AddMcp(options =>
{
    options.ResourceMetadata = new ProtectedResourceMetadata
    {
        AuthorizationServers = { entra.Authority },
        ScopesSupported = [$"api://{entra.ClientId}/{entra.RequiredScope}", $"api://{entra.ClientId}/{entra.WriteScope}"]
    };
});

builder.Services.AddAuthorization();
builder.Services.AddSingleton<EmployeeStore>();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ScopeChallengeMiddleware>();
app.MapMcp("/mcp").RequireAuthorization();
app.Run();
