using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.AI;

static class Magentic
{
    const string Task =
        "Plan a one day team offsite in Vienna for 12 people with a total budget of 1500 EUR. " +
        "Propose a venue, catering and a four item agenda, then verify the total stays within budget.";

    const string Feedback = "Keep the venue under 400 EUR and add a short team building activity to the agenda.";

    [Description("Adds up line item costs and returns the total in EUR.")]
    static string SumCosts([Description("Cost of each line item in EUR")] double[] items) => $"{items.Sum():F2} EUR";

    static Workflow BuildWorkflow(Func<string, string, string?, IList<AITool>?, AIAgent> makeAgent, bool enablePlanReview)
    {
        AIAgent researcher = makeAgent("Researcher", "You propose venue, catering and agenda options with plausible prices. You do not add up costs.", "Finds options and prices", null);
        AIAgent analyst = makeAgent("Analyst", "You verify budgets. Always call SumCosts for the total.", "Checks totals against the budget", [AIFunctionFactory.Create(SumCosts)]);
        AIAgent manager = makeAgent("Manager", "You coordinate the team to complete the task efficiently.", "Plans and delegates", null);
        return AgentWorkflowBuilder
            .CreateMagenticBuilderWith(manager)
            .AddParticipants(researcher, analyst)
            .WithMaxRounds(12)
            .WithMaxStalls(2)
            .WithMaxResets(1)
            .RequirePlanSignoff(enablePlanReview)
            .Build();
    }

    static string Describe(WorkflowEvent evt) => evt switch
    {
        MagenticProgressLedgerUpdatedEvent ledger => $"[PROGRESS_LEDGER_UPDATED] satisfied={ledger.ProgressLedger.IsRequestSatisfied} next={ledger.ProgressLedger.NextSpeaker}",
        MagenticPlanCreatedEvent created => $"[PLAN_CREATED] {created.FullTaskLedger.Text.Length} characters, mentions 400 EUR: {created.FullTaskLedger.Text.Contains("400")}",
        MagenticReplannedEvent replanned => $"[REPLANNED] {replanned.FullTaskLedger.Text.Length} characters, mentions 400 EUR: {replanned.FullTaskLedger.Text.Contains("400")}",
        _ => "",
    };

    static async Task<string> RunWithReview(Workflow workflow)
    {
        int revisions = 0;
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new ChatMessage(ChatRole.User, Task));
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is MagenticOrchestratorEvent)
            {
                Console.WriteLine(Describe(evt));
            }
            else if (evt is RequestInfoEvent info && info.Request.TryGetDataAs(out MagenticPlanReviewRequest? request))
            {
                Console.WriteLine($"plan under review, revision {revisions}: stalled={request.IsStalled}");
                MagenticPlanReviewResponse response = revisions++ == 0 ? request.Revise(Feedback) : request.Approve();
                await run.SendResponseAsync(info.Request.CreateResponse(response));
            }
            else if (evt is WorkflowOutputEvent output and not AgentResponseUpdateEvent)
            {
                return output.Data switch
                {
                    ChatMessage message => message.Text,
                    IEnumerable<ChatMessage> messages => messages.Last().Text,
                    var other => other?.ToString() ?? "",
                };
            }
        }
        return "";
    }

    public static async Task Run(bool useResponses)
    {
        Console.WriteLine($"Magentic on {(useResponses ? "a Responses" : "a Chat Completions")} client");
        Workflow workflow = BuildWorkflow(useResponses ? Agents.ResponsesAgent : Agents.ChatCompletionsAgent, enablePlanReview: true);
        Console.WriteLine(await RunWithReview(workflow));
    }
}
