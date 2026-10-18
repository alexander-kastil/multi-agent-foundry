using AgentFwWorkflows.Models;
using Azure.AI.Agents.Persistent;
using Azure.Identity;

namespace AgentFwWorkflows.Workflows;

public class AgentsInWorkflow : WorkflowRunnerBase
{
    public AgentsInWorkflow(AppConfig config) : base(config)
    {
    }

    public override async Task RunAsync()
    {
        PrintHeader(
            "🤖 AGENTS INSIDE WORKFLOWS - INVOICE BUILDER",
            @"✨ This demo shows AI AGENTS integrated into workflow steps:
   • Agent analyzes invoice data and provides insights
   • Agent makes business decisions about processing
   • Agent generates personalized client communications
   • Agent creates executive summaries

🔄 Workflow Pattern:
   Select → Analyze → Decide → Communicate → Summarize
   🤖      🤖       🤖         🤖           🤖
   (All middle steps use AI agents for intelligent processing)"
        );

        // Check Azure AI configuration
        if (string.IsNullOrEmpty(Config.AzureAIProjectEndpoint) || 
            string.IsNullOrEmpty(Config.AzureAIModelDeploymentName))
        {
            Console.WriteLine("❌ Azure AI configuration missing. Please check your appsettings.json file.");
            Console.WriteLine("   Required: AzureAIProjectEndpoint, AzureAIModelDeploymentName");
            return;
        }

        try
        {
            await RunAgentWorkflowAsync();

            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("Demo completed! Agents successfully integrated into workflow processing.");
            Console.WriteLine(new string('=', 80));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error running agent workflow: {ex.Message}");
            Console.WriteLine("\nNote: This demo requires Azure AI Foundry project configuration.");
            Console.WriteLine("Some features may not be available without proper credentials.");
        }
    }

    private async Task RunAgentWorkflowAsync()
    {
        // Step 1: Select Invoice
        var invoice = await SelectInvoiceAsync();

        // Step 2: Analyze with Agent
        var analysis = await AnalyzeInvoiceWithAgentAsync(invoice);

        // Step 3: Make Decision with Agent
        var decision = await MakeDecisionWithAgentAsync(invoice, analysis);

        // Step 4: Generate Communication with Agent
        var communication = await GenerateCommunicationWithAgentAsync(invoice, analysis, decision);

        // Step 5: Create Summary with Agent
        await CreateSummaryWithAgentAsync(invoice, analysis, decision, communication);
    }

    private async Task<InvoiceData> SelectInvoiceAsync()
    {
        InvoiceUtils.PrintStep(1, "SELECT INVOICE");

        await Task.Delay(100);

        var csvPath = Path.Combine(DataPath, "invoices.csv");
        var invoices = InvoiceUtils.ReadInvoicesCsv(csvPath);

        Console.WriteLine($"Loaded {invoices.Count} invoices");

        // For simplicity, select first invoice
        var invoice = invoices[0];

        Console.WriteLine($"\nSelected: {invoice.InvoiceId} - {invoice.ClientName}");
        Console.WriteLine($"Amount: ${invoice.Subtotal:F2}");

        InvoiceUtils.LogAction($"Selected invoice {invoice.InvoiceId} for agent processing", LogsPath);

        return invoice;
    }

    private async Task<string> AnalyzeInvoiceWithAgentAsync(InvoiceData invoice)
    {
        InvoiceUtils.PrintStep(2, "AGENT ANALYSIS");

        Console.WriteLine($"🤖 Agent analyzing invoice {invoice.InvoiceId}...");

        await Task.Delay(100);

        var config = InvoiceConfig.FromEnvironment();
        var totals = InvoiceUtils.CalculateInvoiceTotals(invoice, config);

        // Simulated agent analysis (in real implementation, would call Azure AI)
        var analysis = $@"INVOICE ANALYSIS REPORT
========================
Invoice: {invoice.InvoiceId}
Client: {invoice.ClientName}
Amount: ${invoice.Subtotal:F2}
Status: {(invoice.IsPreferred ? "Preferred Client" : "Standard Client")}

Financial Assessment:
- Subtotal: ${totals.Subtotal:F2}
- Applied Discounts: ${totals.TotalDiscount:F2}
- Tax Amount: ${totals.Tax:F2}
- Total Due: ${totals.Total:F2}

Risk Assessment: {(totals.Subtotal > 5000 ? "MEDIUM" : "LOW")}
- Client has {(invoice.IsPreferred ? "excellent" : "good")} payment history
- Invoice amount is {(totals.Subtotal > config.HighValueThreshold ? "above" : "within")} normal range
- Recommended action: {(invoice.IsPreferred ? "PRIORITY" : "APPROVE")}

Business Insights:
- {(invoice.IsPreferred ? "VIP client - ensure expedited processing" : "Standard processing recommended")}
- {(totals.TotalDiscount > 0 ? $"Discounts applied: ${totals.TotalDiscount:F2}" : "No discounts applicable")}
- Credit check: {(invoice.Subtotal < 10000 ? "APPROVED" : "REVIEW REQUIRED")}";

        Console.WriteLine($"\n📊 Agent Analysis Results:");
        Console.WriteLine(new string('─', 80));
        Console.WriteLine(analysis);
        Console.WriteLine(new string('─', 80));

        InvoiceUtils.LogAction($"Agent analyzed invoice {invoice.InvoiceId}", LogsPath);

        return analysis;
    }

    private async Task<string> MakeDecisionWithAgentAsync(InvoiceData invoice, string analysis)
    {
        InvoiceUtils.PrintStep(3, "AGENT DECISION MAKING");

        Console.WriteLine($"🤖 Agent making decision for invoice {invoice.InvoiceId}...");

        await Task.Delay(100);

        // Simulated agent decision
        var decision = invoice.IsPreferred ? "PRIORITY" : "APPROVE";
        var reasoning = invoice.IsPreferred
            ? "Preferred client with excellent history - fast-track for priority processing"
            : "Standard client with good payment record - approve for normal processing";

        var decisionText = $@"BUSINESS DECISION
================
Action: {decision}
Reasoning: {reasoning}

Processing Path: {decision.ToLower()}
Timeline: {(decision == "PRIORITY" ? "24 hours" : "72 hours")}
Special Instructions: {(invoice.IsPreferred ? "Assign to senior account manager" : "Standard workflow")}";

        Console.WriteLine($"\n⚖️  Agent Decision:");
        Console.WriteLine(new string('─', 80));
        Console.WriteLine(decisionText);
        Console.WriteLine(new string('─', 80));

        InvoiceUtils.LogAction($"Agent decided {decision} processing for {invoice.InvoiceId}", LogsPath);

        return decision;
    }

    private async Task<string> GenerateCommunicationWithAgentAsync(InvoiceData invoice, string analysis, string decision)
    {
        InvoiceUtils.PrintStep(4, "AGENT COMMUNICATION");

        Console.WriteLine($"🤖 Agent generating communication for {invoice.ClientName}...");

        await Task.Delay(100);

        var config = InvoiceConfig.FromEnvironment();
        var totals = InvoiceUtils.CalculateInvoiceTotals(invoice, config);

        // Simulated agent communication
        var communication = $@"Dear {invoice.ClientName},

Thank you for your continued business with {config.CompanyName}. We are pleased to confirm receipt of your order.

Invoice Details:
- Invoice Number: {invoice.InvoiceId}
- Date: {invoice.Date}
- Amount: ${totals.Total:F2}

{(invoice.IsPreferred ? @"As a valued preferred client, we have applied your loyalty discount to this order. 
Your invoice has been fast-tracked for priority processing." : @"Your invoice has been processed and is ready for review.")}

{(totals.TotalDiscount > 0 ? $"Total savings on this order: ${totals.TotalDiscount:F2}" : "")}

Payment Information:
- Total Due: ${totals.Total:F2}
- Payment Terms: Net 30
- Due Date: {DateTime.Parse(invoice.Date).AddDays(30):yyyy-MM-dd}

Should you have any questions, please don't hesitate to contact your account manager.

Best regards,
{config.CompanyName} Billing Team";

        Console.WriteLine($"\n📧 Generated Communication:");
        Console.WriteLine(new string('─', 80));
        Console.WriteLine(communication);
        Console.WriteLine(new string('─', 80));

        InvoiceUtils.LogAction($"Agent generated communication for {invoice.InvoiceId}", LogsPath);

        return communication;
    }

    private async Task CreateSummaryWithAgentAsync(InvoiceData invoice, string analysis, string decision, string communication)
    {
        InvoiceUtils.PrintStep(5, "AGENT SUMMARY");

        Console.WriteLine($"🤖 Agent creating executive summary...");

        await Task.Delay(100);

        var config = InvoiceConfig.FromEnvironment();
        var totals = InvoiceUtils.CalculateInvoiceTotals(invoice, config);

        var summary = $@"EXECUTIVE SUMMARY - INVOICE {invoice.InvoiceId}
{'='*80}

CLIENT INFORMATION:
- Name: {invoice.ClientName}
- Email: {invoice.ClientEmail}
- Status: {(invoice.IsPreferred ? "Preferred Client (VIP)" : "Standard Client")}

FINANCIAL OVERVIEW:
- Subtotal: ${totals.Subtotal:F2}
- Discounts Applied: ${totals.TotalDiscount:F2}
- Tax: ${totals.Tax:F2}
- Total Due: ${totals.Total:F2}

PROCESSING DECISION: {decision}
- Path: {decision.ToLower()}
- Priority: {(decision == "PRIORITY" ? "High" : "Normal")}
- Estimated Completion: {(decision == "PRIORITY" ? "24 hours" : "72 hours")}

KEY HIGHLIGHTS:
• {(invoice.IsPreferred ? "VIP client requiring premium service" : "Standard client with routine processing")}
• {(totals.TotalDiscount > 0 ? $"Customer savings: ${totals.TotalDiscount:F2}" : "No discounts applied")}
• {(totals.Subtotal > 5000 ? "High-value transaction - additional review recommended" : "Normal transaction value")}

NEXT STEPS:
1. Client communication sent
2. {(decision == "PRIORITY" ? "Fast-track processing initiated" : "Standard workflow processing")}
3. Payment tracking scheduled for 30-day terms

---
Generated by AI Agent Workflow System
Processed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";

        Console.WriteLine($"\n📋 Executive Summary:");
        Console.WriteLine(new string('─', 80));
        Console.WriteLine(summary);
        Console.WriteLine(new string('─', 80));

        // Save comprehensive report
        InvoiceUtils.EnsureDirectories(OutputPath, LogsPath);

        var fullReport = $@"
EXECUTIVE SUMMARY - INVOICE {invoice.InvoiceId}
{new string('=', 80)}

CLIENT INFORMATION:
- Name: {invoice.ClientName}
- Email: {invoice.ClientEmail}
- Preferred: {(invoice.IsPreferred ? "Yes" : "No")}

FINANCIAL DETAILS:
- Subtotal: ${totals.Subtotal:F2}
- Total Due: ${totals.Total:F2}
- Processing Path: {decision}

AGENT ANALYSIS:
{analysis}

AGENT DECISION:
Decision: {decision}

CLIENT COMMUNICATION:
{communication}

EXECUTIVE SUMMARY:
{summary}

{new string('=', 80)}
";

        var filepath = InvoiceUtils.SaveInvoiceFile($"{invoice.InvoiceId}_agent_report", fullReport, OutputPath);

        InvoiceUtils.LogAction($"Agent workflow completed for {invoice.InvoiceId}", LogsPath);

        PrintWorkflowComplete(
            $"✅ Agent workflow completed! Invoice {invoice.InvoiceId} processed with AI assistance.",
            $"Output: {OutputPath}",
            $"Logs: {LogsPath}",
            "\n🤖 This workflow used AI agents for:",
            "   • Invoice analysis and risk assessment",
            "   • Business decision making",
            "   • Personalized client communication",
            "   • Executive summary generation"
        );
    }
}
