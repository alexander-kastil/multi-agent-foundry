using AgentFwWorkflows.Models;

namespace AgentFwWorkflows.Workflows;

public class SequentialWorkflow : WorkflowRunnerBase
{
    private string? selectedInvoiceId;

    public SequentialWorkflow(AppConfig config) : base(config)
    {
    }

    public override async Task RunAsync()
    {
        PrintHeader(
            "INVOICE BUILDER - INTERACTIVE SEQUENTIAL WORKFLOW",
            @"✨ This demo shows a sequential workflow with INTERACTIVE steps:
   • You select ONE invoice to process
   • Each workflow step pauses for you to review
   • Press ENTER to proceed to the next step
   • See intermediate results at each stage

📋 Workflow Steps:
   1. Load Configuration → Shows tax rates and discounts
   2. Read & Select Invoice → Choose from menu
   3. Calculate Totals → See breakdown of amounts
   4. Render Invoice → Preview formatted invoice
   5. Save Invoice → Write to output file"
        );

        while (true)
        {
            await RunWorkflowOnceAsync();

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.Write("\n🔄 Process another invoice? (y/n): ");
            var choice = Console.ReadLine()?.Trim().ToLower();

            if (choice != "y")
            {
                Console.WriteLine("\n👋 Thank you for using Invoice Builder!");
                Console.WriteLine(new string('=', 80));
                break;
            }

            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("🔄 RESTARTING WORKFLOW...");
            Console.WriteLine(new string('=', 80));
        }
    }

    private async Task RunWorkflowOnceAsync()
    {
        // Step 1: Load Configuration
        var config = await LoadConfigurationAsync();
        
        // Step 2: Read and Select Invoice
        var invoice = await ReadInvoiceDataAsync(config);
        
        // Step 3: Calculate Totals
        var totals = await CalculateTotalsAsync(config, invoice);
        
        // Step 4: Render Invoice
        var invoiceText = await RenderInvoiceAsync(invoice, totals, config);
        
        // Step 5: Save Invoice
        await SaveInvoiceAsync(invoice, totals, invoiceText);
    }

    private async Task<InvoiceConfig> LoadConfigurationAsync()
    {
        InvoiceUtils.PrintStep(1, "LOAD CONFIGURATION");
        Console.WriteLine("🔧 Loading configuration...");

        await Task.Delay(100); // Simulate async operation

        var config = InvoiceConfig.FromEnvironment();

        Console.WriteLine($"\n✅ Configuration loaded successfully!");
        Console.WriteLine($"   📊 Tax Rate: {config.TaxRate * 100}%");
        Console.WriteLine($"   💰 High Value Threshold: ${config.HighValueThreshold:F2}");
        Console.WriteLine($"   🎁 High Value Discount: {config.HighValueDiscount * 100}%");
        Console.WriteLine($"   ⭐ Preferred Client Discount: {config.PreferredClientDiscount * 100}%");

        InvoiceUtils.LogAction($"Configuration loaded: {config.TaxRate}", LogsPath);

        InvoiceUtils.WaitForUser("continue to STEP 2 - Read Invoice Data");

        return config;
    }

    private async Task<InvoiceData> ReadInvoiceDataAsync(InvoiceConfig config)
    {
        InvoiceUtils.PrintStep(2, "READ INVOICE DATA & SELECT");
        Console.WriteLine("📂 Reading invoices from CSV file...");

        await Task.Delay(100); // Simulate async operation

        var csvPath = Path.Combine(DataPath, "invoices.csv");
        var allInvoices = InvoiceUtils.ReadInvoicesCsv(csvPath);

        Console.WriteLine($"\n✅ Loaded {allInvoices.Count} invoices from {Path.GetFileName(csvPath)}");

        // Let user select invoice
        selectedInvoiceId = InvoiceUtils.ShowMenu(allInvoices);
        var selectedInvoice = allInvoices.First(inv => inv.InvoiceId == selectedInvoiceId);

        Console.WriteLine($"\n✅ Selected Invoice: {selectedInvoice.InvoiceId}");
        Console.WriteLine($"   Client: {selectedInvoice.ClientName}");
        Console.WriteLine($"   Email: {selectedInvoice.ClientEmail}");
        Console.WriteLine($"   Item: {selectedInvoice.ItemDescription}");
        Console.WriteLine($"   Quantity: {selectedInvoice.Quantity}");
        Console.WriteLine($"   Unit Price: ${selectedInvoice.UnitPrice:F2}");
        Console.WriteLine($"   Subtotal: ${selectedInvoice.Subtotal:F2}");
        Console.WriteLine($"   Preferred Client: {(selectedInvoice.IsPreferred ? "⭐ YES" : "❌ NO")}");

        InvoiceUtils.LogAction($"Selected invoice {selectedInvoiceId} for processing", LogsPath);

        InvoiceUtils.WaitForUser("continue to STEP 3 - Calculate Totals");

        return selectedInvoice;
    }

    private async Task<InvoiceTotals> CalculateTotalsAsync(InvoiceConfig config, InvoiceData invoice)
    {
        InvoiceUtils.PrintStep(3, "CALCULATE TOTALS");

        await Task.Delay(100); // Simulate async operation

        Console.WriteLine($"🧮 Calculating amounts for {invoice.InvoiceId}...");
        Console.WriteLine($"   Starting Subtotal: ${invoice.Subtotal:F2}");

        var totals = InvoiceUtils.CalculateInvoiceTotals(invoice, config);

        Console.WriteLine($"\n✅ Calculation Complete!");
        Console.WriteLine($"\n   {"Item",-30} {"Amount",15}");
        Console.WriteLine($"   {new string('-', 30)} {new string('-', 15)}");
        Console.WriteLine($"   {"Subtotal",-30} ${totals.Subtotal,14:N2}");

        if (totals.HighValueDiscount > 0)
        {
            Console.WriteLine($"   {"High Value Discount (5%)",-30} -${totals.HighValueDiscount,13:N2}");
        }

        if (totals.PreferredDiscount > 0)
        {
            Console.WriteLine($"   {"Preferred Discount (3%)",-30} -${totals.PreferredDiscount,13:N2}");
        }

        if (totals.TotalDiscount > 0)
        {
            Console.WriteLine($"   {new string('-', 30)} {new string('-', 15)}");
            Console.WriteLine($"   {"Amount After Discounts",-30} ${totals.AmountAfterDiscount,14:N2}");
        }

        Console.WriteLine($"   {"Tax (10%)",-30} ${totals.Tax,14:N2}");
        Console.WriteLine($"   {new string('=', 30)} {new string('=', 15)}");
        Console.WriteLine($"   {"💰 TOTAL DUE",-30} ${totals.Total,14:N2}");
        Console.WriteLine($"   {new string('=', 30)} {new string('=', 15)}");

        InvoiceUtils.LogAction($"Calculated totals for {invoice.InvoiceId}: ${totals.Total:F2}", LogsPath);

        InvoiceUtils.WaitForUser("continue to STEP 4 - Render Invoice");

        return totals;
    }

    private async Task<string> RenderInvoiceAsync(InvoiceData invoice, InvoiceTotals totals, InvoiceConfig config)
    {
        InvoiceUtils.PrintStep(4, "RENDER INVOICE");

        await Task.Delay(100); // Simulate async operation

        Console.WriteLine($"🖨️  Rendering invoice {invoice.InvoiceId} as formatted text...");

        var invoiceText = InvoiceUtils.RenderInvoiceText(invoice, totals, config);

        Console.WriteLine($"\n✅ Invoice rendered successfully! ({invoiceText.Length} characters)");

        // Show preview
        Console.WriteLine($"\n{new string('─', 80)}");
        Console.WriteLine("📄 INVOICE PREVIEW:");
        Console.WriteLine(new string('─', 80));
        Console.WriteLine(invoiceText);
        Console.WriteLine(new string('─', 80));

        InvoiceUtils.LogAction($"Rendered invoice {invoice.InvoiceId}", LogsPath);

        InvoiceUtils.WaitForUser("continue to STEP 5 - Save Invoice");

        return invoiceText;
    }

    private async Task SaveInvoiceAsync(InvoiceData invoice, InvoiceTotals totals, string invoiceText)
    {
        InvoiceUtils.PrintStep(5, "SAVE INVOICE");

        await Task.Delay(100); // Simulate async operation

        InvoiceUtils.EnsureDirectories(OutputPath, LogsPath);

        Console.WriteLine($"💾 Saving invoice {invoice.InvoiceId} to disk...");

        var filepath = InvoiceUtils.SaveInvoiceFile(invoice.InvoiceId, invoiceText, OutputPath);

        Console.WriteLine($"\n✅ Invoice saved successfully!");
        Console.WriteLine($"   📁 Location: {filepath}");
        Console.WriteLine($"   📊 Client: {invoice.ClientName}");
        Console.WriteLine($"   💵 Amount: ${totals.Total:F2}");

        InvoiceUtils.LogAction($"Saved invoice {invoice.InvoiceId} to {filepath}", LogsPath);

        PrintWorkflowComplete(
            $"✅ Sequential workflow completed! Invoice {invoice.InvoiceId} processed successfully.",
            $"Output: {OutputPath}",
            $"Logs: {LogsPath}"
        );
    }
}
