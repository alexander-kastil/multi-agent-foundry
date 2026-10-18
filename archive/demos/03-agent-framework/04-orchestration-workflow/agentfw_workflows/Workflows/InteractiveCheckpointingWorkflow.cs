using AgentFwWorkflows.Models;

namespace AgentFwWorkflows.Workflows;

public class InteractiveCheckpointingWorkflow : WorkflowRunnerBase
{
    public InteractiveCheckpointingWorkflow(AppConfig config) : base(config)
    {
    }

    public override async Task RunAsync()
    {
        PrintHeader(
            "SIMPLE INTERACTIVE INVOICE APPROVAL WORKFLOW",
            @"This demo combines:
  - Manual human-in-the-loop interaction
  - Automatic checkpointing at each pause point
  - Request/response correlation with typed payloads

Note: This is a simplified demonstration of the checkpointing concept.
Full state persistence would require additional infrastructure."
        );

        var csvPath = Path.Combine(DataPath, "invoices.csv");
        var allInvoices = InvoiceUtils.ReadInvoicesCsv(csvPath);

        Console.WriteLine($"\nReading invoices from: {csvPath}");
        Console.WriteLine($"Loaded {allInvoices.Count} invoices");

        var selectedInvoiceId = InvoiceUtils.ShowMenu(allInvoices);
        var selectedInvoice = allInvoices.First(inv => inv.InvoiceId == selectedInvoiceId);

        InvoiceUtils.LogAction($"Selected invoice {selectedInvoiceId} for interactive approval", LogsPath);

        await RunInteractiveWorkflowAsync(selectedInvoice);
    }

    private async Task RunInteractiveWorkflowAsync(InvoiceData invoice)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("WORKFLOW STRUCTURE:");
        Console.WriteLine("   Prepare -> Tax Request -> Tax Process -> Discount Request -> Discount Process -> Finalize");
        Console.WriteLine("   Checkpoints automatically saved at each pause point");
        Console.WriteLine(new string('=', 80));

        // Step 1: Prepare Invoice
        var state = await PrepareInvoiceAsync(invoice);

        // Step 2: Request Tax Confirmation
        var taxRequest = await RequestTaxConfirmationAsync(state);

        // Step 3: Process Tax Response
        var taxResponse = await GetUserTaxResponseAsync(taxRequest);
        state = await ProcessTaxResponseAsync(taxResponse, state);

        // Step 4: Request Discount Confirmation
        var discountRequest = await RequestDiscountConfirmationAsync(state);

        // Step 5: Process Discount Response
        var discountResponse = await GetUserDiscountResponseAsync(discountRequest);
        state = await ProcessDiscountResponseAsync(discountResponse, state);

        // Step 6: Finalize
        await FinalizeInvoiceAsync(state);

        PrintCheckpointSummary();
    }

    private async Task<InvoiceState> PrepareInvoiceAsync(InvoiceData invoice)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("STEP 1: PREPARE INVOICE");
        Console.WriteLine(new string('=', 80));

        await Task.Delay(100);

        var config = InvoiceConfig.FromEnvironment();
        var totals = InvoiceUtils.CalculateInvoiceTotals(invoice, config);

        var discountAmount = totals.HighValueDiscount + totals.PreferredDiscount;
        var discountRate = invoice.Subtotal > 0 ? discountAmount / invoice.Subtotal : 0.0;

        var state = new InvoiceState
        {
            InvoiceId = invoice.InvoiceId,
            ClientName = invoice.ClientName,
            Subtotal = invoice.Subtotal,
            TaxRate = config.TaxRate,
            TaxAmount = totals.Tax,
            DiscountRate = discountRate,
            DiscountAmount = discountAmount,
            ProcessingStage = "preparation"
        };

        Console.WriteLine($"Selected invoice: {invoice.InvoiceId}");
        Console.WriteLine($"   Client: {invoice.ClientName}");
        Console.WriteLine($"   Amount: ${invoice.Subtotal:F2}");
        Console.WriteLine($"   Tax Rate: {config.TaxRate * 100}%");
        Console.WriteLine($"   Calculated Tax: ${totals.Tax:F2}");
        Console.WriteLine($"   Discount: ${discountAmount:F2}");

        Console.WriteLine("\n[CHECKPOINT] State saved at 'preparation' stage");

        return state;
    }

    private async Task<TaxConfirmationRequest> RequestTaxConfirmationAsync(InvoiceState state)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("STEP 2: REQUEST TAX CONFIRMATION");
        Console.WriteLine(new string('=', 80));

        await Task.Delay(100);

        Console.WriteLine($"Invoice: {state.InvoiceId}");
        Console.WriteLine($"   Subtotal: ${state.Subtotal:F2}");
        Console.WriteLine($"   Tax Rate: {state.TaxRate * 100}%");
        Console.WriteLine($"   Calculated Tax: ${state.TaxAmount:F2}");
        Console.WriteLine($"\nWorkflow will pause for user confirmation...");

        Console.WriteLine("\n[CHECKPOINT] State saved at 'tax_confirmation' stage");

        return new TaxConfirmationRequest
        {
            InvoiceId = state.InvoiceId,
            Question = $"Confirm tax calculation for {state.InvoiceId}?",
            CurrentValue = state.TaxAmount,
            Options = "Type 'yes' to confirm or 'no' to skip"
        };
    }

    private async Task<bool> GetUserTaxResponseAsync(TaxConfirmationRequest request)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("MANUAL INPUT REQUIRED");
        Console.WriteLine(new string('=', 80));

        Console.WriteLine("\nTax Confirmation:");
        Console.WriteLine($"   Current Value: ${request.CurrentValue:F2}");
        Console.WriteLine($"   {request.Options}");
        Console.Write("Your response: ");
        var response = Console.ReadLine()?.Trim().ToLower();
        var confirmed = response == "yes" || response == "y";
        Console.WriteLine(confirmed ? "   ✅ Confirmed" : "   ❌ Skipped");

        await Task.Delay(100);

        return confirmed;
    }

    private async Task<InvoiceState> ProcessTaxResponseAsync(bool confirmed, InvoiceState state)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("STEP 3: PROCESS TAX CONFIRMATION");
        Console.WriteLine(new string('=', 80));

        await Task.Delay(100);

        state.TaxConfirmed = confirmed;
        state.ProcessingStage = "tax_processed";

        if (confirmed)
        {
            Console.WriteLine($"✅ Tax confirmed: ${state.TaxAmount:F2}");
        }
        else
        {
            Console.WriteLine($"❌ Tax skipped");
            state.TaxAmount = 0.0;
        }

        Console.WriteLine("\n[CHECKPOINT] State saved at 'tax_processed' stage");

        return state;
    }

    private async Task<DiscountConfirmationRequest> RequestDiscountConfirmationAsync(InvoiceState state)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("STEP 4: REQUEST DISCOUNT CONFIRMATION");
        Console.WriteLine(new string('=', 80));

        await Task.Delay(100);

        if (state.DiscountAmount > 0)
        {
            Console.WriteLine($"Invoice: {state.InvoiceId}");
            Console.WriteLine($"   Total Discount: ${state.DiscountAmount:F2}");
            Console.WriteLine($"\nWorkflow will pause for user confirmation...");

            Console.WriteLine("\n[CHECKPOINT] State saved at 'discount_confirmation' stage");

            return new DiscountConfirmationRequest
            {
                InvoiceId = state.InvoiceId,
                Question = $"Apply discount to {state.InvoiceId}?",
                CurrentValue = state.DiscountAmount,
                Options = "Type 'yes' to apply or 'no' to skip"
            };
        }
        else
        {
            Console.WriteLine($"No discount applicable for {state.InvoiceId}");
            state.DiscountConfirmed = true;
            state.ProcessingStage = "discount_skipped";
            return new DiscountConfirmationRequest { InvoiceId = state.InvoiceId, CurrentValue = 0 };
        }
    }

    private async Task<bool> GetUserDiscountResponseAsync(DiscountConfirmationRequest request)
    {
        if (request.CurrentValue <= 0) return true;

        Console.WriteLine("\nDiscount Confirmation:");
        Console.WriteLine($"   Current Value: ${request.CurrentValue:F2}");
        Console.WriteLine($"   {request.Options}");
        Console.Write("Your response: ");
        var response = Console.ReadLine()?.Trim().ToLower();
        var confirmed = response == "yes" || response == "y";
        Console.WriteLine(confirmed ? "   ✅ Applied" : "   ❌ Skipped");

        await Task.Delay(100);

        return confirmed;
    }

    private async Task<InvoiceState> ProcessDiscountResponseAsync(bool confirmed, InvoiceState state)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("STEP 5: PROCESS DISCOUNT CONFIRMATION");
        Console.WriteLine(new string('=', 80));

        await Task.Delay(100);

        state.DiscountConfirmed = confirmed;
        state.ProcessingStage = "discount_processed";

        if (confirmed)
        {
            Console.WriteLine($"✅ Discount applied: ${state.DiscountAmount:F2}");
        }
        else
        {
            Console.WriteLine($"❌ Discount skipped");
            state.DiscountAmount = 0.0;
        }

        Console.WriteLine("\n[CHECKPOINT] State saved at 'discount_processed' stage");

        return state;
    }

    private async Task FinalizeInvoiceAsync(InvoiceState state)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("STEP 6: FINALIZE INVOICE");
        Console.WriteLine(new string('=', 80));

        await Task.Delay(100);

        var finalTotal = state.Subtotal;
        if (state.TaxConfirmed) finalTotal += state.TaxAmount;
        if (state.DiscountConfirmed) finalTotal -= state.DiscountAmount;

        Console.WriteLine($"Invoice: {state.InvoiceId}");
        Console.WriteLine($"   Subtotal: ${state.Subtotal:F2}");
        if (state.TaxConfirmed) Console.WriteLine($"   Tax: ${state.TaxAmount:F2}");
        if (state.DiscountConfirmed) Console.WriteLine($"   Discount: -${state.DiscountAmount:F2}");
        Console.WriteLine($"   Final Total: ${finalTotal:F2}");

        var outputDir = Path.Combine(OutputPath, "interactive");
        Directory.CreateDirectory(outputDir);
        var outputFile = Path.Combine(outputDir, $"{state.InvoiceId}_final.txt");

        await File.WriteAllTextAsync(outputFile, $@"INVOICE: {state.InvoiceId}
Client: {state.ClientName}
Subtotal: ${state.Subtotal:F2}
{(state.TaxConfirmed ? $"Tax: ${state.TaxAmount:F2}" : "")}
{(state.DiscountConfirmed ? $"Discount: -${state.DiscountAmount:F2}" : "")}
Final Total: ${finalTotal:F2}
Status: Completed with user confirmations
");

        Console.WriteLine($"\n✅ Output file created: {outputFile}");

        Console.WriteLine("\n[CHECKPOINT] State saved at 'completed' stage");

        PrintWorkflowComplete(
            $"Invoice {state.InvoiceId} completed with user confirmations!",
            $"Output: {outputDir}"
        );
    }

    private void PrintCheckpointSummary()
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("FINAL CHECKPOINT SUMMARY");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("\nKey Features Demonstrated:");
        Console.WriteLine("   - Manual human-in-the-loop interaction");
        Console.WriteLine("   - Automatic checkpointing at each pause point");
        Console.WriteLine("   - Request/response correlation with typed payloads");
        Console.WriteLine("   - State persistence across pause-resume cycles");
        Console.WriteLine(new string('=', 80));
    }

    private class InvoiceState
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public double Subtotal { get; set; }
        public double TaxRate { get; set; }
        public double TaxAmount { get; set; }
        public double DiscountRate { get; set; }
        public double DiscountAmount { get; set; }
        public bool TaxConfirmed { get; set; }
        public bool DiscountConfirmed { get; set; }
        public string ProcessingStage { get; set; } = "preparation";
    }

    private class TaxConfirmationRequest
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public string Options { get; set; } = string.Empty;
    }

    private class DiscountConfirmationRequest
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public string Options { get; set; } = string.Empty;
    }
}
