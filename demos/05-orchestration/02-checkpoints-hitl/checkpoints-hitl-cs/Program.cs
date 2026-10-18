using System.Globalization;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Checkpointing;
using CheckpointsHitlCs;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var store = new FileSystemJsonCheckpointStore(new DirectoryInfo("checkpoints"));
var checkpointManager = CheckpointManager.CreateJson(store);

Workflow BuildWorkflow()
{
    var intake = new Intake();
    var approvalPort = RequestPort.Create<ApprovalRequest, string>("approval");
    var finalize = new Finalize();
    return new WorkflowBuilder(intake)
        .AddEdge(intake, approvalPort)
        .AddEdge(approvalPort, finalize)
        .WithOutputFrom(finalize)
        .Build();
}

CheckpointInfo? waiting = null;
await using (StreamingRun run = await InProcessExecution.RunStreamingAsync(BuildWorkflow(), "Team offsite dinner for 12 people, 640 EUR", checkpointManager))
{
    await foreach (WorkflowEvent evt in run.WatchStreamAsync(blockOnPendingRequest: false))
    {
        if (evt is RequestInfoEvent request)
        {
            Console.WriteLine($"waiting for a human, request {request.Request.RequestId}");
        }
        if (evt is SuperStepCompletedEvent completed)
        {
            waiting = completed.CompletionInfo?.Checkpoint;
            Console.WriteLine($"superstep {completed.StepNumber} checkpoint {waiting?.CheckpointId[..8]}");
        }
    }
}

Console.WriteLine("the first run is gone, resuming into a new workflow instance");

await using StreamingRun resumed = await InProcessExecution.ResumeStreamingAsync(BuildWorkflow(), waiting!, checkpointManager);
await foreach (WorkflowEvent evt in resumed.WatchStreamAsync())
{
    if (evt is RequestInfoEvent request)
    {
        Console.WriteLine($"re-emitted request {request.Request.RequestId}");
        await resumed.SendResponseAsync(request.Request.CreateResponse("approve"));
    }
    if (evt is WorkflowOutputEvent output)
    {
        Console.WriteLine(output.Data);
    }
}
