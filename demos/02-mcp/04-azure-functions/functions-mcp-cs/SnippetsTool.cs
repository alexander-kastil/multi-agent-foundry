using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace FunctionsMcp;

public class SnippetsTool
{
    private const string BlobPath = "snippets/{mcptoolargs.snippetname}.json";

    [Function(nameof(GetSnippet))]
    public string? GetSnippet(
        [McpToolTrigger("get_snippet", "Retrieves a snippet by name from blob storage.")] ToolInvocationContext context,
        [McpToolProperty("snippetname", "The name of the snippet.", isRequired: true)] string name,
        [BlobInput(BlobPath)] string? snippetContent) => snippetContent;

    [Function(nameof(SaveSnippet))]
    [BlobOutput(BlobPath)]
    public string SaveSnippet(
        [McpToolTrigger("save_snippet", "Saves a snippet under a name into blob storage.")] ToolInvocationContext context,
        [McpToolProperty("snippetname", "The name of the snippet.", isRequired: true)] string name,
        [McpToolProperty("snippet", "The content of the snippet.", isRequired: true)] string snippet) => snippet;
}
