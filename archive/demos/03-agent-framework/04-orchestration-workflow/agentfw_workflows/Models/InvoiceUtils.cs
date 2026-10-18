using System.Text;

namespace AgentFwWorkflows.Models;

public static class InvoiceUtils
{
    public static List<InvoiceData> ReadInvoicesCsv(string csvPath)
    {
        var invoices = new List<InvoiceData>();
        
        var lines = File.ReadAllLines(csvPath);
        if (lines.Length < 2) return invoices;

        var headers = lines[0].Split(',');
        var invoiceIdIdx = Array.IndexOf(headers, "invoice_id");
        var clientNameIdx = Array.IndexOf(headers, "client_name");
        var clientEmailIdx = Array.IndexOf(headers, "client_email");
        var isPreferredIdx = Array.IndexOf(headers, "is_preferred");
        var itemDescIdx = Array.IndexOf(headers, "item_description");
        var quantityIdx = Array.IndexOf(headers, "quantity");
        var unitPriceIdx = Array.IndexOf(headers, "unit_price");
        var dateIdx = Array.IndexOf(headers, "date");

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            if (values.Length < headers.Length) continue;

            invoices.Add(new InvoiceData
            {
                InvoiceId = values[invoiceIdIdx],
                ClientName = values[clientNameIdx],
                ClientEmail = values[clientEmailIdx],
                IsPreferred = values[isPreferredIdx].Equals("true", StringComparison.OrdinalIgnoreCase),
                ItemDescription = values[itemDescIdx],
                Quantity = int.Parse(values[quantityIdx]),
                UnitPrice = double.Parse(values[unitPriceIdx]),
                Date = values[dateIdx]
            });
        }

        return invoices;
    }

    public static InvoiceTotals CalculateInvoiceTotals(InvoiceData invoice, InvoiceConfig config)
    {
        var subtotal = invoice.Subtotal;

        var highValueDiscount = 0.0;
        if (subtotal >= config.HighValueThreshold)
        {
            highValueDiscount = subtotal * config.HighValueDiscount;
        }

        var preferredDiscount = 0.0;
        if (invoice.IsPreferred)
        {
            preferredDiscount = subtotal * config.PreferredClientDiscount;
        }

        var totalDiscount = highValueDiscount + preferredDiscount;
        var amountAfterDiscount = subtotal - totalDiscount;
        var tax = amountAfterDiscount * config.TaxRate;
        var total = amountAfterDiscount + tax;

        return new InvoiceTotals
        {
            Subtotal = subtotal,
            HighValueDiscount = highValueDiscount,
            PreferredDiscount = preferredDiscount,
            TotalDiscount = totalDiscount,
            AmountAfterDiscount = amountAfterDiscount,
            Tax = tax,
            Total = total
        };
    }

    public static string RenderInvoiceText(InvoiceData invoice, InvoiceTotals totals, InvoiceConfig config)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine(new string('=', 80));
        sb.AppendLine(config.CompanyName.PadLeft(40 + config.CompanyName.Length / 2).PadRight(80));
        sb.AppendLine(config.CompanyAddress.PadLeft(40 + config.CompanyAddress.Length / 2).PadRight(80));
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();
        sb.AppendLine($"INVOICE: {invoice.InvoiceId}");
        sb.AppendLine($"Date: {invoice.Date}");
        sb.AppendLine();
        sb.AppendLine("Bill To:");
        sb.AppendLine($"  {invoice.ClientName}");
        sb.AppendLine($"  {invoice.ClientEmail}");
        if (invoice.IsPreferred)
        {
            sb.AppendLine("  ⭐ PREFERRED CLIENT");
        }
        sb.AppendLine();
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"{"DESCRIPTION",-40} {"QTY",-10} {"PRICE",-15} {"AMOUNT",15}");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"{invoice.ItemDescription,-40} {invoice.Quantity,-10} ${invoice.UnitPrice,-14:F2} ${totals.Subtotal,14:F2}");
        sb.AppendLine();
        sb.AppendLine($"{"Subtotal:",-65} ${totals.Subtotal,14:F2}");

        if (totals.HighValueDiscount > 0)
        {
            sb.AppendLine($"{"High Value Discount (5%):",-65} -${totals.HighValueDiscount,13:F2}");
        }

        if (totals.PreferredDiscount > 0)
        {
            sb.AppendLine($"{"Preferred Client Discount (3%):",-65} -${totals.PreferredDiscount,13:F2}");
        }

        if (totals.TotalDiscount > 0)
        {
            sb.AppendLine($"{"Amount After Discount:",-65} ${totals.AmountAfterDiscount,14:F2}");
        }

        sb.AppendLine($"{"Tax (10%):",-65} ${totals.Tax,14:F2}");
        sb.AppendLine(new string('-', 80));
        sb.AppendLine($"{"TOTAL DUE:",-65} ${totals.Total,14:F2}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();
        sb.AppendLine("Thank you for your business!");
        sb.AppendLine();

        return sb.ToString();
    }

    public static string SaveInvoiceFile(string invoiceId, string content, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var filepath = Path.Combine(outputDir, "invoices", $"{invoiceId}.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(filepath)!);
        File.WriteAllText(filepath, content);
        return filepath;
    }

    public static bool ArchiveOldInvoice(string invoiceId, string outputDir, string archiveDir)
    {
        var filepath = Path.Combine(outputDir, "invoices", $"{invoiceId}.txt");
        if (!File.Exists(filepath)) return false;

        Directory.CreateDirectory(archiveDir);
        var archivePath = Path.Combine(archiveDir, $"{invoiceId}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.Move(filepath, archivePath);
        return true;
    }

    public static void LogAction(string message, string logsDir)
    {
        Directory.CreateDirectory(logsDir);
        var logFile = Path.Combine(logsDir, $"workflow_{DateTime.Now:yyyyMMdd}.log");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        File.AppendAllText(logFile, $"[{timestamp}] {message}\n");
    }

    public static void EnsureDirectories(params string[] dirs)
    {
        foreach (var dir in dirs)
        {
            Directory.CreateDirectory(dir);
        }
    }

    public static void PrintStep(int stepNumber, string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"STEP {stepNumber}: {title}");
        Console.WriteLine(new string('=', 80));
    }

    public static void WaitForUser(string message)
    {
        Console.WriteLine();
        Console.WriteLine(new string('-', 80));
        Console.Write($"⏸️  Press ENTER to {message} ▶️  ");
        Console.ReadLine();
        Console.WriteLine(new string('-', 80));
        Console.WriteLine();
    }

    public static string ShowMenu(List<InvoiceData> invoices)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("📋 AVAILABLE INVOICES");
        Console.WriteLine(new string('=', 80));

        for (int i = 0; i < invoices.Count; i++)
        {
            var inv = invoices[i];
            var preferredBadge = inv.IsPreferred ? "⭐" : "  ";
            Console.WriteLine($"{i + 1}. {preferredBadge} {inv.InvoiceId} - {inv.ClientName}");
            Console.WriteLine($"   Amount: ${inv.Subtotal:F2} | Date: {inv.Date}");
            Console.WriteLine();
        }

        while (true)
        {
            Console.Write($"Select invoice (1-{invoices.Count}): ");
            var choice = Console.ReadLine();
            if (int.TryParse(choice, out var idx) && idx >= 1 && idx <= invoices.Count)
            {
                return invoices[idx - 1].InvoiceId;
            }
            Console.WriteLine($"❌ Please enter a number between 1 and {invoices.Count}");
        }
    }
}
