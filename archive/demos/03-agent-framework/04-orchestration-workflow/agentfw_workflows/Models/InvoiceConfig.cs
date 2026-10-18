namespace AgentFwWorkflows.Models;

public class InvoiceConfig
{
    public double TaxRate { get; set; } = 0.10;
    public double HighValueThreshold { get; set; } = 5000.00;
    public double HighValueDiscount { get; set; } = 0.05;
    public double PreferredClientDiscount { get; set; } = 0.03;
    public string CompanyName { get; set; } = "TechServices Inc.";
    public string CompanyAddress { get; set; } = "123 Business St, Tech City, TC 12345";

    public static InvoiceConfig FromEnvironment()
    {
        return new InvoiceConfig
        {
            TaxRate = double.TryParse(Environment.GetEnvironmentVariable("INVOICE_TAX_RATE"), out var taxRate) ? taxRate : 0.10,
            HighValueThreshold = double.TryParse(Environment.GetEnvironmentVariable("INVOICE_HIGH_VALUE_THRESHOLD"), out var threshold) ? threshold : 5000.00,
            HighValueDiscount = double.TryParse(Environment.GetEnvironmentVariable("INVOICE_HIGH_VALUE_DISCOUNT"), out var hvDiscount) ? hvDiscount : 0.05,
            PreferredClientDiscount = double.TryParse(Environment.GetEnvironmentVariable("INVOICE_PREFERRED_DISCOUNT"), out var prefDiscount) ? prefDiscount : 0.03,
            CompanyName = Environment.GetEnvironmentVariable("INVOICE_COMPANY_NAME") ?? "TechServices Inc.",
            CompanyAddress = Environment.GetEnvironmentVariable("INVOICE_COMPANY_ADDRESS") ?? "123 Business St, Tech City, TC 12345"
        };
    }
}
