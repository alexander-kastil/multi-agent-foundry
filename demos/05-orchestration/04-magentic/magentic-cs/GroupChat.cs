using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

static class GroupChat
{
    const string Task = "Write a one sentence slogan for an expense app that scans receipts with the phone camera.";

    static async Task PrintRun(Workflow workflow)
    {
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, new List<ChatMessage> { new(ChatRole.User, Task) });
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is WorkflowOutputEvent output and not AgentResponseUpdateEvent)
            {
                foreach (ChatMessage message in output.As<List<ChatMessage>>()!.Where(m => m.Role == ChatRole.Assistant))
                    Console.WriteLine($"[{message.AuthorName}] {message.Text}");
                return;
            }
        }
    }

    public static async Task Run()
    {
        AIAgent writer = Agents.ResponsesAgent("Writer", "You write one slogan per turn. Revise it when the reviewer gives feedback.", "Writes slogans");
        AIAgent reviewer = Agents.ResponsesAgent("Reviewer", "You review the latest slogan in one sentence. Say APPROVED when it is clear and short.", "Reviews slogans");

        Console.WriteLine("Selection function, four turns");
        Workflow selectorChat = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 4 })
            .AddParticipants(writer, reviewer)
            .Build();
        await PrintRun(selectorChat);

        Console.WriteLine("\nAgent orchestrator picks the speaker");
        AIAgent orchestrator = Agents.ResponsesAgent(
            "Orchestrator",
            "You coordinate a slogan team. Let Writer go first, then Reviewer. Finish as soon as Reviewer says APPROVED. " +
            "Reply with exactly one word: Writer, Reviewer or FINISH.",
            "Selects the next speaker");
        Workflow agentChat = AgentWorkflowBuilder
            .CreateGroupChatBuilderWith(agents => new OrchestratorGroupChatManager(agents, orchestrator) { MaximumIterationCount = 6 })
            .AddParticipants(writer, reviewer)
            .Build();
        await PrintRun(agentChat);
    }
}

class OrchestratorGroupChatManager(IReadOnlyList<AIAgent> agents, AIAgent orchestrator) : GroupChatManager
{
    string? decision;

    async ValueTask<string> Decide(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken)
    {
        string transcript = string.Join("\n", history.Select(message => $"{message.AuthorName ?? message.Role.Value}: {message.Text}"));
        AgentResponse response = await orchestrator.RunAsync($"Conversation so far:\n{transcript}\n\nWho speaks next?", cancellationToken: cancellationToken);
        return decision = response.Text.Trim();
    }

    protected override async ValueTask<AIAgent> SelectNextAgentAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        string next = decision ?? await Decide(history, cancellationToken);
        decision = null;
        return agents.FirstOrDefault(agent => string.Equals(agent.Name, next, StringComparison.OrdinalIgnoreCase)) ?? agents[0];
    }

    protected override async ValueTask<bool> ShouldTerminateAsync(IReadOnlyList<ChatMessage> history, CancellationToken cancellationToken = default)
    {
        if (await base.ShouldTerminateAsync(history, cancellationToken))
            return true;
        return history.Any(message => message.Role == ChatRole.Assistant) && (await Decide(history, cancellationToken)).Equals("FINISH", StringComparison.OrdinalIgnoreCase);
    }
}
