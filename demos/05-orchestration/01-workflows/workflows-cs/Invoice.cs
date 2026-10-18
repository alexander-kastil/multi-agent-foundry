namespace WorkflowsCs;

internal sealed record Invoice(string Number, string Vendor, string Description, decimal Amount);

internal sealed record Check(string Name, bool Passed);

internal static class Samples
{
    public static readonly string[] RawInvoices =
    [
        "INV-001,Contoso Ltd,Cloud hosting,420.00",
        "INV-002,Fabrikam Inc,Office furniture,2750.50",
        "INV-003,Northwind Traders,Security audit,18400.00",
        "INV-004,Adatum Corp,Unreadable scan,-1",
    ];
}
