using System;
using System.IO;
using System.Text;
using ConnectedAgents.Models;

namespace ConnectedAgentsAI.Utilities;

public static class OutputHelper
{
    public static string SaveTicketFile(
        string outputFolderPath,
        string userQuery,
        string agentResponse,
        AiAppConfig config,
        int tokenUsageIn = 0,
        int tokenUsageOut = 0)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var ticketId = timestamp;

        var tokenUsageTotal = tokenUsageIn + tokenUsageOut;

        var sb = new StringBuilder();
        sb.AppendLine($"# Ticket {ticketId}");
        sb.AppendLine();
        sb.AppendLine("## Configuration");
        sb.AppendLine($"- **Agent Name**: {config.AgentName}");
        sb.AppendLine($"- **Model**: {config.Model}");
        sb.AppendLine($"- **Project Connection**: {config.ProjectConnectionString}");
        sb.AppendLine($"- **Timestamp**: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## User Query");
        sb.AppendLine("```");
        sb.AppendLine(userQuery);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Agent Response");
        sb.AppendLine("```");
        sb.AppendLine(agentResponse);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Token Usage");
        sb.AppendLine($"- **Input Tokens**: {tokenUsageIn}");
        sb.AppendLine($"- **Output Tokens**: {tokenUsageOut}");
        sb.AppendLine($"- **Total Tokens**: {tokenUsageTotal}");

        Directory.CreateDirectory(outputFolderPath);
        var filePath = Path.Combine(outputFolderPath, $"ticket-{ticketId}.md");
        File.WriteAllText(filePath, sb.ToString());
        return Path.GetFullPath(filePath);
    }
}
