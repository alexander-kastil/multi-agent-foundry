using Microsoft.Agents.AI;
using Microsoft.Agents.AI.DurableTask;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace DurableAgents;

public record StartRequest(string Topic);

public record Announcement(string Title, string Body);

public record Decision(bool Approved, string Feedback = "");

public class AnnouncementApproval(ILogger<AnnouncementApproval> logger)
{
    private const string Writer = "AnnouncementWriter";
    private const string ApprovalEvent = "ApprovalDecision";

    [Function(nameof(RunAnnouncementApproval))]
    public static async Task<Announcement> RunAnnouncementApproval([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var topic = context.GetInput<StartRequest>()!.Topic;
        DurableAIAgent writer = context.GetAgent(Writer);
        AgentSession session = await writer.CreateSessionAsync();

        var draft = await writer.RunAsync<Announcement>($"Write a release announcement about {topic}.", session);
        var announcement = draft.Result;

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            context.SetCustomStatus($"waiting for review, round {attempt}");
            await context.CallActivityAsync(nameof(RequestReview), announcement);

            Decision decision;
            try
            {
                decision = await context.WaitForExternalEvent<Decision>(ApprovalEvent, TimeSpan.FromHours(72));
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("No review decision within 72 hours");
            }

            if (decision.Approved)
            {
                await context.CallActivityAsync(nameof(Publish), announcement);
                context.SetCustomStatus("published");
                return announcement;
            }

            var revised = await writer.RunAsync<Announcement>(
                $"The reviewer rejected it. Rewrite it with this feedback: {decision.Feedback}", session);
            announcement = revised.Result;
        }

        throw new InvalidOperationException("Announcement was not approved after 3 rounds");
    }

    [Function(nameof(RequestReview))]
    public void RequestReview([ActivityTrigger] Announcement announcement) =>
        logger.LogInformation("REVIEW REQUESTED: {Title}", announcement.Title);

    [Function(nameof(Publish))]
    public void Publish([ActivityTrigger] Announcement announcement) =>
        logger.LogInformation("PUBLISHED: {Title}", announcement.Title);

    [Function("start")]
    public static async Task<HttpResponseData> Start(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "approvals/run")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var request = await req.ReadFromJsonAsync<StartRequest>();
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(RunAnnouncementApproval), request);
        var response = req.CreateResponse(System.Net.HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new { instanceId });
        return response;
    }

    [Function("decide")]
    public static async Task<HttpResponseData> Decide(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "approvals/decision/{instanceId}")] HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        var decision = await req.ReadFromJsonAsync<Decision>();
        await client.RaiseEventAsync(instanceId, ApprovalEvent, decision);
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new { sent = decision!.Approved });
        return response;
    }

    [Function("status")]
    public static async Task<HttpResponseData> Status(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "approvals/status/{instanceId}")] HttpRequestData req,
        string instanceId,
        [DurableClient] DurableTaskClient client)
    {
        var state = await client.GetInstanceAsync(instanceId, getInputsAndOutputs: true);
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        await response.WriteAsJsonAsync(new
        {
            runtimeStatus = state?.RuntimeStatus.ToString(),
            workflowStatus = state?.ReadCustomStatusAs<string>(),
            output = state?.RuntimeStatus == OrchestrationRuntimeStatus.Completed ? state.ReadOutputAs<Announcement>() : null
        });
        return response;
    }
}
