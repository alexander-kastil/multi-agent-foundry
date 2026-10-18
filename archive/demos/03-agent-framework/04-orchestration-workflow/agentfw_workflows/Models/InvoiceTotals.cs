namespace AgentFwWorkflows.Models;

public class InvoiceTotals
{
    public double Subtotal { get; set; }
    public double HighValueDiscount { get; set; }
    public double PreferredDiscount { get; set; }
    public double TotalDiscount { get; set; }
    public double AmountAfterDiscount { get; set; }
    public double Tax { get; set; }
    public double Total { get; set; }
}
