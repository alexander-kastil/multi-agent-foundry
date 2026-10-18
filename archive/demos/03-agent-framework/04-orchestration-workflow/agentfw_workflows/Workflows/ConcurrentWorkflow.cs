using AgentFwWorkflows.Models;

namespace AgentFwWorkflows.Workflows;

public class ConcurrentWorkflow : WorkflowRunnerBase
{
    private string? selectedInvoiceId;

    public ConcurrentWorkflow(AppConfig config) : base(config)
    {
    }

    public override async Task RunAsync()
    {
        PrintHeader(
            "CONCURRENT WORKFLOW - INVOICE BUILDER",
            @"This demo shows PARALLEL PROCESSING with interactive steps:
   • You select ONE invoice to process
   • THREE tasks run SIMULTANEOUSLY:
     1. Calculate totals (amounts, discounts, tax)
     2. Prepare client information (name, status, email)
     3. Perform credit check (score, limit, approval)
   • Results MERGE when all three tasks complete
   • Final invoice rendered and saved

Workflow Pattern:
   Dispatcher -> [Totals Calculator + Client Preparer + Credit Checker] -> Merger -> Renderer
                +----------- PARALLEL EXECUTION -----------+"
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
        // Step 1: Load and Select
        var (config, invoice) = await LoadAndSelectAsync();

        InvoiceUtils.WaitForUser("start PARALLEL processing");

        // Step 2: Run three parallel tasks
        var totalsTask = CalculateTotalsAsync(invoice, config);
        var clientTask = PrepareClientInfoAsync(invoice);
        var creditTask = PerformCreditCheckAsync(invoice);

        await Task.WhenAll(totalsTask, clientTask, creditTask);

        var totals = await totalsTask;
        var clientInfo = await clientTask;
        var creditCheck = await creditTask;

        // Step 3: Merge results
        Console.WriteLine($"\n[MERGER] All three parallel tasks complete - merging results...");

        InvoiceUtils.WaitForUser("proceed to RENDERING");

        // Step 4: Render and save
        await RenderAndSaveAsync(invoice, config, totals, clientInfo, creditCheck);
    }

    private async Task<(InvoiceConfig, InvoiceData)> LoadAndSelectAsync()
    {
        InvoiceUtils.PrintStep(1, "LOAD & SELECT INVOICE");

        await Task.Delay(100);

        var config = InvoiceConfig.FromEnvironment();
        var csvPath = Path.Combine(DataPath, "invoices.csv");
        var allInvoices = InvoiceUtils.ReadInvoicesCsv(csvPath);

        Console.WriteLine($"Loaded {allInvoices.Count} invoices");

        selectedInvoiceId = InvoiceUtils.ShowMenu(allInvoices);
        var selectedInvoice = allInvoices.First(inv => inv.InvoiceId == selectedInvoiceId);

        Console.WriteLine($"\nSelected: {selectedInvoice.InvoiceId} - {selectedInvoice.ClientName}");
        Console.WriteLine($"   Amount: ${selectedInvoice.Subtotal:F2}");
        Console.WriteLine($"   Preferred: {(selectedInvoice.IsPreferred ? "YES" : "NO")}");

        InvoiceUtils.LogAction($"Selected invoice {selectedInvoiceId} for parallel processing", LogsPath);

        return (config, selectedInvoice);
    }

    private async Task<InvoiceTotals> CalculateTotalsAsync(InvoiceData invoice, InvoiceConfig config)
    {
        Console.WriteLine($"\n[TOTALS] Calculating totals for {invoice.InvoiceId}...");

        await Task.Delay(100); // Simulate processing time

        var totals = InvoiceUtils.CalculateInvoiceTotals(invoice, config);

        Console.WriteLine($"   ✅ Calculation Complete!");
        Console.WriteLine($"      Subtotal: ${totals.Subtotal:F2}");
        Console.WriteLine($"      Discounts: -${totals.TotalDiscount:F2}");
        Console.WriteLine($"      Tax: ${totals.Tax:F2}");
        Console.WriteLine($"      Total: ${totals.Total:F2}");

        return totals;
    }

    private async Task<Dictionary<string, string>> PrepareClientInfoAsync(InvoiceData invoice)
    {
        Console.WriteLine($"\n[CLIENT] Preparing client info for {invoice.InvoiceId}...");

        await Task.Delay(500); // Simulate processing time

        var clientInfo = new Dictionary<string, string>
        {
            ["name"] = invoice.ClientName,
            ["email"] = invoice.ClientEmail,
            ["is_preferred"] = invoice.IsPreferred.ToString(),
            ["status"] = invoice.IsPreferred ? "VIP" : "Standard",
            ["greeting"] = $"Dear {invoice.ClientName},",
            ["account_manager"] = $"AM-{invoice.ClientName[..Math.Min(3, invoice.ClientName.Length)].ToUpper()}",
            ["last_order_date"] = invoice.IsPreferred ? "2024-12-01" : "2024-11-15"
        };

        Console.WriteLine($"   ✅ Client Info Ready!");
        Console.WriteLine($"      Name: {clientInfo["name"]}");
        Console.WriteLine($"      Status: {clientInfo["status"]}");
        Console.WriteLine($"      Email: {clientInfo["email"]}");
        Console.WriteLine($"      Account Manager: {clientInfo["account_manager"]}");

        return clientInfo;
    }

    private async Task<Dictionary<string, object>> PerformCreditCheckAsync(InvoiceData invoice)
    {
        Console.WriteLine($"\n[CREDIT] Processing credit check for {invoice.InvoiceId}...");

        await Task.Delay(800); // Simulate credit check processing time

        var invoiceAmount = invoice.Subtotal;
        var isPreferred = invoice.IsPreferred;

        // Credit scoring logic
        int creditScore;
        double creditLimit;
        string riskLevel;

        if (isPreferred)
        {
            creditScore = 850;
            creditLimit = 50000;
            riskLevel = "LOW";
        }
        else if (invoiceAmount > 5000)
        {
            creditScore = 720;
            creditLimit = 25000;
            riskLevel = "MEDIUM";
        }
        else
        {
            creditScore = 650;
            creditLimit = 10000;
            riskLevel = "MEDIUM";
        }

        var approved = invoiceAmount <= creditLimit;

        var creditCheck = new Dictionary<string, object>
        {
            ["credit_score"] = creditScore,
            ["credit_limit"] = creditLimit,
            ["risk_level"] = riskLevel,
            ["approved"] = approved,
            ["invoice_amount"] = invoiceAmount,
            ["available_credit"] = approved ? creditLimit - invoiceAmount : 0,
            ["check_timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        var status = approved ? "APPROVED" : "DECLINED";
        Console.WriteLine($"   ✅ Credit Check Complete!");
        Console.WriteLine($"      Status: {status}");
        Console.WriteLine($"      Score: {creditScore}");
        Console.WriteLine($"      Limit: ${creditLimit:N0}");
        Console.WriteLine($"      Risk: {riskLevel}");

        return creditCheck;
    }

    private async Task RenderAndSaveAsync(
        InvoiceData invoice,
        InvoiceConfig config,
        InvoiceTotals totals,
        Dictionary<string, string> clientInfo,
        Dictionary<string, object> creditCheck)
    {
        InvoiceUtils.PrintStep(3, "RENDER & SAVE");

        await Task.Delay(100);

        InvoiceUtils.EnsureDirectories(OutputPath, LogsPath);

        Console.WriteLine($"Rendering invoice {invoice.InvoiceId}...");

        // Render invoice text
        var invoiceText = InvoiceUtils.RenderInvoiceText(invoice, totals, config);

        // Add credit check information
        var approved = (bool)creditCheck["approved"];
        var creditScore = creditCheck["credit_score"];
        var creditLimit = (double)creditCheck["credit_limit"];
        var riskLevel = creditCheck["risk_level"];
        var invoiceAmount = (double)creditCheck["invoice_amount"];
        var availableCredit = (double)creditCheck["available_credit"];
        var checkTimestamp = creditCheck["check_timestamp"];

        var creditInfo = $@"
CREDIT CHECK RESULTS:
====================
Status: {(approved ? "APPROVED" : "DECLINED")}
Credit Score: {creditScore}
Credit Limit: ${creditLimit:N2}
Risk Level: {riskLevel}
Invoice Amount: ${invoiceAmount:N2}
Available Credit: ${availableCredit:N2}
Check Date: {checkTimestamp}

";

        // Add client information
        var clientInfoText = $@"
CLIENT INFORMATION:
==================
Name: {clientInfo["name"]}
Email: {clientInfo["email"]}
Status: {clientInfo["status"]}
Account Manager: {clientInfo["account_manager"]}
Last Order: {clientInfo["last_order_date"]}

";

        // Combine all information
        var fullInvoiceText = invoiceText + creditInfo + clientInfoText;

        // Show preview
        Console.WriteLine($"\n{new string('-', 80)}");
        Console.WriteLine("INVOICE PREVIEW:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine(fullInvoiceText);
        Console.WriteLine(new string('-', 80));

        // Save to file
        var filepath = InvoiceUtils.SaveInvoiceFile(invoice.InvoiceId, fullInvoiceText, OutputPath);

        Console.WriteLine($"\n✅ Invoice saved successfully!");
        Console.WriteLine($"   Location: {filepath}");
        Console.WriteLine($"   Client: {clientInfo["name"]} ({clientInfo["status"]})");
        Console.WriteLine($"   Amount: ${totals.Total:F2}");
        Console.WriteLine($"   Credit: {(approved ? "APPROVED" : "DECLINED")} (Score: {creditScore})");

        InvoiceUtils.LogAction($"Rendered and saved {invoice.InvoiceId} using concurrent workflow with credit check", LogsPath);

        PrintWorkflowComplete(
            $"Concurrent workflow completed! Invoice {invoice.InvoiceId} processed with 3 parallel tasks.",
            $"Output: {OutputPath}",
            $"Logs: {LogsPath}",
            "Note: Three parallel executors ran concurrently for better performance!",
            "Each invoice now includes totals, client info, AND credit check results!"
        );
    }
}
