using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

static class HandoffCollaboration
{
    public static async Task Run()
    {
        AIAgent secAnalyzer = Agents.Make(
            "sec_filing_analyzer",
            "You extract financial metrics from SEC filings. When foreign revenue exceeds 30 percent, hand off to international_risk_assessor. Do not summarize prior work, return findings in full structured form.",
            "Extracts metrics from SEC filings");
        AIAgent internationalRisk = Agents.Make(
            "international_risk_assessor",
            "You assess currency exposure and geopolitical risk. The full conversation is available to you, build on it. Return JSON with currency_risk_score (0-10), hedging_recommendations and exposure_by_market. Out of scope: valuation, credit metrics.",
            "Assesses currency and geopolitical risk");
        AIAgent creditAnalyst = Agents.Make(
            "credit_analyst",
            "You analyze debt structure and credit metrics. When you find foreign currency obligations, hand off to international_risk_assessor.",
            "Analyzes debt structure and credit metrics");

        Workflow workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(secAnalyzer)
            .WithHandoffs(secAnalyzer, [internationalRisk, creditAnalyst])
            .WithHandoffs(creditAnalyst, [internationalRisk])
            .Build();

        List<ChatMessage> input = [new(ChatRole.User, "Analyze ACME Corp's 10-K for investment risks. Foreign revenue is 60 percent across Brazil, India and Turkey.")];
        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is WorkflowOutputEvent output and not AgentResponseUpdateEvent)
            {
                List<ChatMessage> messages = output.As<List<ChatMessage>>()!;
                List<string> authors = [];
                foreach (ChatMessage message in messages.Where(m => m.Role == ChatRole.Assistant && !string.IsNullOrWhiteSpace(m.Text)))
                    if (!authors.Contains(message.AuthorName!))
                        authors.Add(message.AuthorName!);
                Console.WriteLine($"Agents that spoke in order: [{string.Join(", ", authors.Select(author => $"'{author}'"))}]");
                Console.WriteLine($"Final message: {messages.Last(m => !string.IsNullOrWhiteSpace(m.Text)).Text}");
                return;
            }
        }
    }
}
