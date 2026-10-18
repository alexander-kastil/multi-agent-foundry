using System.Diagnostics;
using System.Globalization;
using Microsoft.Agents.AI.Workflows;
using WorkflowsCs;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var parse = new ParseInvoice();
var classify = new Classify();
var reject = new Decision("reject", "rejected, amount is not valid");
var autoApprove = new Decision("auto_approve", "auto-approved");
var managerReview = new Decision("manager_review", "sent to the manager");
var directorReview = new Decision("director_review", "sent to the director");

var routing = new WorkflowBuilder(parse)
    .AddEdge<Invoice>(parse, classify, condition: invoice => invoice is { Amount: > 0 })
    .AddEdge<Invoice>(parse, reject, condition: invoice => invoice is { Amount: <= 0 })
    .AddSwitch(classify, switchBuilder => switchBuilder
        .AddCase<Invoice>(invoice => invoice!.Amount < 1000, autoApprove)
        .AddCase<Invoice>(invoice => invoice!.Amount < 10000, managerReview)
        .WithDefault(directorReview))
    .WithOutputFrom(reject, autoApprove, managerReview, directorReview)
    .Build();

foreach (var line in Samples.RawInvoices)
{
    await using Run run = await InProcessExecution.RunAsync(routing, line);
    foreach (var evt in run.NewEvents.OfType<WorkflowOutputEvent>())
    {
        Console.WriteLine(evt.Data);
    }
}

var duplicate = new DuplicateCheck();
var vendor = new VendorCheck();
var budget = new BudgetCheck();
var aggregate = new Aggregate();

var fanOut = new WorkflowBuilder(parse)
    .AddFanOutEdge(parse, [duplicate, vendor, budget])
    .AddFanInBarrierEdge([duplicate, vendor, budget], aggregate)
    .WithOutputFrom(aggregate)
    .Build();

var stopwatch = Stopwatch.StartNew();
await using Run fanRun = await InProcessExecution.RunAsync(fanOut, Samples.RawInvoices[2]);
foreach (var evt in fanRun.NewEvents)
{
    if (evt is SuperStepStartedEvent superstep)
    {
        Console.WriteLine($"superstep {superstep.StepNumber}");
    }
    else if (evt is ExecutorInvokedEvent invoked)
    {
        Console.WriteLine($"  invoked {invoked.ExecutorId}");
    }
    else if (evt is WorkflowOutputEvent output)
    {
        Console.WriteLine($"output: {output.Data}");
    }
}
Console.WriteLine($"total {stopwatch.Elapsed.TotalSeconds:F1}s for three one-second checks");

Console.WriteLine(routing.ToMermaidString());
