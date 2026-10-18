using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace FunctionsMcp;

public class HelloTool
{
    [Function(nameof(HelloMcp))]
    public string HelloMcp(
        [McpToolTrigger("hello_mcp", "Says hello from an MCP server hosted on Azure Functions.")] ToolInvocationContext context) =>
        "Hello I am MCPTool!";
}
