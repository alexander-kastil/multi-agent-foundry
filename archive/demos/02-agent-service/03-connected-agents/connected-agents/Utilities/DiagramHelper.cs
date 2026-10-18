using System;
using System.IO;
using System.Text;

namespace ConnectedAgents.Utilities;

public static class DiagramHelper
{
    public static string GenerateDiagram(bool verbose)
    {
        if (verbose)
        {
            return @"sequenceDiagram
    participant User
    participant TriageAgent as Triage Agent<br/>(Main Orchestrator)
    participant PriorityAgent as Priority Agent<br/>(Urgency Assessment)
    participant TeamAgent as Team Agent<br/>(Team Assignment)
    participant EffortAgent as Effort Agent<br/>(Complexity Estimation)

    User->>TriageAgent: Submit ticket description
    Note over TriageAgent: Parse ticket content<br/>Identify key requirements<br/>Plan assessment strategy

    TriageAgent->>PriorityAgent: Request priority assessment
    Note over PriorityAgent: Analyze ticket urgency:<br/>• User-facing/blocking → High<br/>• Time-sensitive → Medium<br/>• Cosmetic/non-urgent → Low
    PriorityAgent-->>TriageAgent: Return: Priority level + rationale

    TriageAgent->>TeamAgent: Request team assignment
    Note over TeamAgent: Match ticket to team:<br/>• Frontend (UI/UX issues)<br/>• Backend (API/server logic)<br/>• Infrastructure (deployment/ops)<br/>• Marketing (content/campaigns)
    TeamAgent-->>TriageAgent: Return: Team name + rationale

    TriageAgent->>EffortAgent: Request effort estimation
    Note over EffortAgent: Estimate work complexity:<br/>• Small: <1 day<br/>• Medium: 2-3 days<br/>• Large: Multi-day/cross-team
    EffortAgent-->>TriageAgent: Return: Effort level + justification

    Note over TriageAgent: Synthesize all assessments<br/>Generate comprehensive triage report
    TriageAgent-->>User: Complete triage analysis<br/>(Priority + Team + Effort)";
        }

        return @"sequenceDiagram
    participant User
    participant TriageAgent
    participant PriorityAgent
    participant TeamAgent
    participant EffortAgent

    User->>TriageAgent: Submit ticket
    TriageAgent->>PriorityAgent: Assess priority
    PriorityAgent-->>TriageAgent: Return priority level
    TriageAgent->>TeamAgent: Determine team
    TeamAgent-->>TriageAgent: Return team assignment
    TriageAgent->>EffortAgent: Estimate effort
    EffortAgent-->>TriageAgent: Return effort estimate
    TriageAgent-->>User: Complete triage analysis";
    }

    public static string SaveDiagramFile(string ticketFolderPath, string ticketPrompt, string resolution, int tokenUsageIn, int tokenUsageOut)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var ticketId = timestamp;

        var simpleDiagram = GenerateDiagram(verbose: false);
        var verboseDiagram = GenerateDiagram(verbose: true);

        var tokenUsageTotal = tokenUsageIn + tokenUsageOut;

        var sb = new StringBuilder();
        sb.AppendLine($"# Ticket {ticketId}");
        sb.AppendLine();
        sb.AppendLine("## Ticket Description");
        sb.AppendLine($"- **Description**: {ticketPrompt}");
        sb.AppendLine($"- **Resolution**: {resolution}");
        sb.AppendLine($"- **Token Usage**: In: {tokenUsageIn}, Out: {tokenUsageOut}, Total: {tokenUsageTotal}");
        sb.AppendLine();
        sb.AppendLine("## Diagram");
        sb.AppendLine("```mermaid");
        sb.AppendLine(simpleDiagram);
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Verbose Diagram");
        sb.AppendLine("```mermaid");
        sb.AppendLine(verboseDiagram);
        sb.AppendLine("```");

        Directory.CreateDirectory(ticketFolderPath);
        var filePath = Path.Combine(ticketFolderPath, $"ticket-{ticketId}.md");
        File.WriteAllText(filePath, sb.ToString());
        return Path.GetFullPath(filePath);
    }
}
