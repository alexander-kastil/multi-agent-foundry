namespace AgentFwWorkflows.Models;

public class InvoiceData
{
    public string InvoiceId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;
    public bool IsPreferred { get; set; }
    public string ItemDescription { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public string Date { get; set; } = string.Empty;

    public double Subtotal => Quantity * UnitPrice;
}
