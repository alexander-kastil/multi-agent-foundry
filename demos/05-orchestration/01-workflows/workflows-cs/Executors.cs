using System.Globalization;
using Microsoft.Agents.AI.Workflows;

namespace WorkflowsCs;

internal sealed partial class ParseInvoice() : Executor("parse_invoice")
{
    [MessageHandler]
    private ValueTask<Invoice> HandleAsync(string line, IWorkflowContext context)
    {
        var parts = line.Split(',');
        return ValueTask.FromResult(new Invoice(parts[0], parts[1], parts[2], decimal.Parse(parts[3], CultureInfo.InvariantCulture)));
    }
}

internal sealed partial class Classify() : Executor("classify")
{
    [MessageHandler]
    private ValueTask<Invoice> HandleAsync(Invoice invoice, IWorkflowContext context) => ValueTask.FromResult(invoice);
}

[YieldsOutput(typeof(string))]
internal sealed partial class Decision(string id, string label) : Executor(id)
{
    [MessageHandler]
    private async ValueTask HandleAsync(Invoice invoice, IWorkflowContext context) =>
        await context.YieldOutputAsync($"{invoice.Number}: {label} ({invoice.Amount:F2})");
}

internal sealed partial class DuplicateCheck() : Executor("duplicate_check")
{
    [MessageHandler]
    private async ValueTask<Check> HandleAsync(Invoice invoice, IWorkflowContext context)
    {
        await Task.Delay(1000);
        return new Check("duplicate", true);
    }
}

internal sealed partial class VendorCheck() : Executor("vendor_check")
{
    [MessageHandler]
    private async ValueTask<Check> HandleAsync(Invoice invoice, IWorkflowContext context)
    {
        await Task.Delay(1000);
        return new Check("vendor", invoice.Vendor != "Adatum Corp");
    }
}

internal sealed partial class BudgetCheck() : Executor("budget_check")
{
    [MessageHandler]
    private async ValueTask<Check> HandleAsync(Invoice invoice, IWorkflowContext context)
    {
        await Task.Delay(1000);
        return new Check("budget", invoice.Amount < 10000);
    }
}

[YieldsOutput(typeof(string))]
internal sealed partial class Aggregate() : Executor("aggregate")
{
    private readonly List<Check> _checks = [];

    [MessageHandler]
    private async ValueTask HandleAsync(Check check, IWorkflowContext context)
    {
        _checks.Add(check);
        if (_checks.Count < 3)
        {
            return;
        }
        var failed = _checks.Where(c => !c.Passed).Select(c => c.Name).ToList();
        _checks.Clear();
        await context.YieldOutputAsync(failed.Count == 0 ? "all checks passed" : $"failed: {string.Join(", ", failed)}");
    }
}
