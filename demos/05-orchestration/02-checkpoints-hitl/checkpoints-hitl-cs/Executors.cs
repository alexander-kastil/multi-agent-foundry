using Microsoft.Agents.AI.Workflows;

namespace CheckpointsHitlCs;

internal sealed record ApprovalRequest(string Summary);

internal sealed partial class Intake() : Executor("intake")
{
    [MessageHandler]
    private ValueTask<ApprovalRequest> HandleAsync(string report, IWorkflowContext context) =>
        ValueTask.FromResult(new ApprovalRequest($"Expense report: {report}"));
}

[YieldsOutput(typeof(string))]
internal sealed partial class Finalize() : Executor("finalize")
{
    [MessageHandler]
    private async ValueTask HandleAsync(string decision, IWorkflowContext context) =>
        await context.YieldOutputAsync($"decision: {decision.ToUpperInvariant()}");
}
