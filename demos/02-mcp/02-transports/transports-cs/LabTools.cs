using System.ComponentModel;
using System.Diagnostics;
using ModelContextProtocol.Server;

namespace Transports;

[McpServerToolType]
public class LabTools
{
    public static string Transport { get; set; } = "stdio";

    [McpServerTool(Name = "server_info")]
    [Description("Reports which process and transport answered the call.")]
    public object ServerInfo() => new { pid = Process.GetCurrentProcess().Id, transport = Transport };

    [McpServerTool(Name = "add")]
    [Description("Adds two integers.")]
    public int Add([Description("First number")] int a, [Description("Second number")] int b) => a + b;
}
