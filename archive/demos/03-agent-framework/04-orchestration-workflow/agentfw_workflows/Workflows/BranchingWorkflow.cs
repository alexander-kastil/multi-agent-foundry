using AgentFwWorkflows.Models;

namespace AgentFwWorkflows.Workflows;

public class BranchingWorkflow : WorkflowRunnerBase
{
    private string? selectedInvoiceId;

    public BranchingWorkflow(AppConfig config) : base(config)
    {
    }

    public override async Task RunAsync()
    {
        PrintHeader(
            "BRANCHING LOGIC WORKFLOW - INVOICE BUILDER",
            @"This demo shows CONDITIONAL BRANCHING with interactive steps:
   • You select ONE invoice to process
   • System analyzes and determines routing path
   • Invoice follows appropriate branch:
     1. Existing file? -> Archive old version first
     2. High value invoice? -> Apply volume discount
     3. Preferred client? -> Apply loyalty discount
     4. Otherwise -> Standard processing
   • Final invoice rendered and saved

Workflow Pattern:
   Loader -> [Archive Check] -> [Value/Preferred/Standard Branches] -> Finalizer
            +-------- CONDITIONAL ROUTING --------+"
        );

        while (true)
        {
            await RunWorkflowOnceAsync();

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.Write("\nProcess another invoice? (y/n): ");
            var choice = Console.ReadLine()?.Trim().ToLower();

            if (choice != "y")
            {
                Console.WriteLine("\nThank you for using Invoice Builder!");
                Console.WriteLine(new string('=', 80));
                break;
            }

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("RESTARTING WORKFLOW...");
            Console.WriteLine(new string('=', 80));
        }
    }

    private async Task RunWorkflowOnceAsync()
    {
        // Step 1: Load and Analyze
        var (config, invoice, decisionType, reason, totals) = await LoadAndAnalyzeAsync();

        // Step 2: Archive if needed
        if (decisionType == "archive_needed")
        {
            decisionType = await HandleArchiveAsync(invoice, config);
        }

        // Step 3: Branch based on decision
        switch (decisionType)
        {
            case "high_value":
                await HandleHighValueAsync(invoice, totals, reason);
                break;
            case "preferred":
                await HandlePreferredAsync(invoice, totals, reason);
                break;
            default:
                await HandleStandardAsync(invoice, totals, reason);
                break;
        }

        // Step 4: Finalize
        await FinalizeAsync(invoice, config, totals, decisionType, reason);
    }

    private async Task<(InvoiceConfig, InvoiceData, string, string, InvoiceTotals)> LoadAndAnalyzeAsync()
    {
        InvoiceUtils.PrintStep(1, "LOAD & SELECT INVOICE");

        await Task.Delay(100);

        var config = InvoiceConfig.FromEnvironment();
        var csvPath = Path.Combine(DataPath, "invoices.csv");
        var allInvoices = InvoiceUtils.ReadInvoicesCsv(csvPath);

        Console.WriteLine($"Loaded {allInvoices.Count} invoices");

        selectedInvoiceId = InvoiceUtils.ShowMenu(allInvoices);
        var invoice = allInvoices.First(inv => inv.InvoiceId == selectedInvoiceId);

        Console.WriteLine($"\nSelected: {invoice.InvoiceId} - {invoice.ClientName}");
        Console.WriteLine($"   Amount: ${invoice.Subtotal:F2}");
        Console.WriteLine($"   Preferred: {(invoice.IsPreferred ? "YES" : "NO")}");

        // Analyze routing
        var (decisionType, reason) = AnalyzeInvoiceRouting(invoice, config);
        var totals = InvoiceUtils.CalculateInvoiceTotals(invoice, config);

        Console.WriteLine($"\nANALYSIS RESULTS:");
        Console.WriteLine($"   Decision Type: {decisionType.ToUpper()}");
        Console.WriteLine($"   Reason: {reason}");

        if (decisionType == "high_value")
        {
            Console.WriteLine($"   High Value Threshold: ${config.HighValueThreshold:F2}");
            Console.WriteLine($"   High Value Discount: ${totals.HighValueDiscount:F2}");
        }
        else if (decisionType == "preferred")
        {
            Console.WriteLine($"   Loyalty Discount: ${totals.PreferredDiscount:F2}");
        }

        InvoiceUtils.LogAction($"Selected and analyzed {selectedInvoiceId}: {decisionType}", LogsPath);

        InvoiceUtils.WaitForUser("start BRANCHING workflow");

        return (config, invoice, decisionType, reason, totals);
    }

    private (string, string) AnalyzeInvoiceRouting(InvoiceData invoice, InvoiceConfig config)
    {
        var totals = InvoiceUtils.CalculateInvoiceTotals(invoice, config);

        // Check if file exists (needs archiving)
        var outputFile = Path.Combine(OutputPath, "invoices", $"{invoice.InvoiceId}.txt");
        if (File.Exists(outputFile))
        {
            return ("archive_needed", "Existing file found - needs archiving");
        }

        // Check high value
        if (totals.Subtotal >= config.HighValueThreshold)
        {
            return ("high_value", $"High value (${totals.Subtotal:F2}) - applying discount");
        }

        // Check preferred client
        if (invoice.IsPreferred)
        {
            return ("preferred", "Preferred client - applying loyalty discount");
        }

        // Default to standard
        return ("standard", "Normal processing");
    }

    private async Task<string> HandleArchiveAsync(InvoiceData invoice, InvoiceConfig config)
    {
        Console.WriteLine($"\n[ARCHIVE BRANCH] {invoice.InvoiceId}");
        Console.WriteLine($"   Reason: Existing file found - needs archiving");

        await Task.Delay(100);

        var archiveDir = Path.Combine(OutputPath, "archive");
        InvoiceUtils.EnsureDirectories(archiveDir);

        var archived = InvoiceUtils.ArchiveOldInvoice(invoice.InvoiceId, OutputPath, archiveDir);

        if (archived)
        {
            Console.WriteLine($"   ✅ Old invoice archived to {archiveDir}");
            InvoiceUtils.LogAction($"Archived old invoice {invoice.InvoiceId}", LogsPath);
        }

        // Re-analyze for next decision (now that archive is done)
        var (decisionType, reason) = AnalyzeInvoiceRouting(invoice, config);

        Console.WriteLine($"   Continuing to next routing decision...");
        Console.WriteLine($"   Next Decision: {decisionType.ToUpper()}");
        Console.WriteLine($"   Reason: {reason}");

        InvoiceUtils.WaitForUser("continue to NEXT BRANCH");

        return decisionType;
    }

    private async Task HandleHighValueAsync(InvoiceData invoice, InvoiceTotals totals, string reason)
    {
        Console.WriteLine($"\n[HIGH VALUE BRANCH] {invoice.InvoiceId}");
        Console.WriteLine($"   Reason: {reason}");
        Console.WriteLine($"   Original Total: ${totals.Total:F2}");
        Console.WriteLine($"   High Value Discount: ${totals.HighValueDiscount:F2}");
        Console.WriteLine($"   ✅ Special processing applied");

        await Task.Delay(100);

        InvoiceUtils.LogAction($"Applied high-value discount to {invoice.InvoiceId}", LogsPath);

        InvoiceUtils.WaitForUser("proceed to FINALIZATION");
    }

    private async Task HandlePreferredAsync(InvoiceData invoice, InvoiceTotals totals, string reason)
    {
        Console.WriteLine($"\n[PREFERRED CLIENT BRANCH] {invoice.InvoiceId}");
        Console.WriteLine($"   Reason: {reason}");
        Console.WriteLine($"   Client: {invoice.ClientName}");
        Console.WriteLine($"   Original Total: ${totals.Total:F2}");
        Console.WriteLine($"   Loyalty Discount: ${totals.PreferredDiscount:F2}");
        Console.WriteLine($"   ✅ Loyalty rewards applied");

        await Task.Delay(100);

        InvoiceUtils.LogAction($"Applied preferred client discount to {invoice.InvoiceId}", LogsPath);

        InvoiceUtils.WaitForUser("proceed to FINALIZATION");
    }

    private async Task HandleStandardAsync(InvoiceData invoice, InvoiceTotals totals, string reason)
    {
        Console.WriteLine($"\n[STANDARD BRANCH] {invoice.InvoiceId}");
        Console.WriteLine($"   Reason: {reason}");
        Console.WriteLine($"   Client: {invoice.ClientName}");
        Console.WriteLine($"   Total: ${totals.Total:F2}");
        Console.WriteLine($"   ✅ Standard processing");

        await Task.Delay(100);

        InvoiceUtils.LogAction($"Standard processing for {invoice.InvoiceId}", LogsPath);

        InvoiceUtils.WaitForUser("proceed to FINALIZATION");
    }

    private async Task FinalizeAsync(InvoiceData invoice, InvoiceConfig config, InvoiceTotals totals, string decisionType, string reason)
    {
        InvoiceUtils.PrintStep(3, "RENDER & SAVE");

        await Task.Delay(100);

        // Render invoice
        var invoiceText = InvoiceUtils.RenderInvoiceText(invoice, totals, config);

        // Add branch information
        var branchInfo = $@"
BRANCHING DECISION:
==================
Decision Type: {decisionType.ToUpper()}
Reason: {reason}
Processing Path: {decisionType.Replace("_", " ")}

";

        var fullInvoiceText = invoiceText + branchInfo;

        // Save to file
        InvoiceUtils.EnsureDirectories(OutputPath, LogsPath);
        var filepath = InvoiceUtils.SaveInvoiceFile(invoice.InvoiceId, fullInvoiceText, OutputPath);

        Console.WriteLine($"Rendering invoice {invoice.InvoiceId}...");

        // Show preview
        Console.WriteLine($"\n{new string('-', 80)}");
        Console.WriteLine("INVOICE PREVIEW:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine(fullInvoiceText);
        Console.WriteLine(new string('-', 80));

        Console.WriteLine($"\n✅ Invoice saved successfully!");
        Console.WriteLine($"   Location: {filepath}");
        Console.WriteLine($"   Branch: {decisionType.ToUpper()}");
        Console.WriteLine($"   Total: ${totals.Total:F2}");

        InvoiceUtils.LogAction($"Finalized {invoice.InvoiceId} via {decisionType} branch", LogsPath);

        PrintWorkflowComplete(
            $"Branching workflow completed! Invoice {invoice.InvoiceId} processed via {decisionType} branch.",
            $"Output: {OutputPath}",
            $"Archive: {Path.Combine(OutputPath, "archive")}",
            $"Logs: {LogsPath}",
            "Note: Invoice followed its appropriate branch based on business rules!"
        );
    }
}
