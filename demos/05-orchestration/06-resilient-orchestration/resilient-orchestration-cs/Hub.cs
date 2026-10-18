using System.ComponentModel;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

static class Hub
{
    const string HubInstructions =
        "You coordinate investment research for Contoso Capital by delegating to specialist agents. " +
        "Always complete market analysis before you invoke risk assessment, and run compliance last. " +
        "Each tool result carries a status; when a spoke did not succeed, say what is missing and continue with what you have. " +
        "Finish with a short research report.";

    [Description("Analyzes current market conditions for the given asset classes and returns a market report.")]
    static async Task<string> InvokeMarketAnalysisAgent([Description("Asset classes to analyze, for example equities")] string[] assetClasses) =>
        JsonSerializer.Serialize(await Resilience.InvokeSpoke("market_analysis", $"Analyze current conditions for: {string.Join(", ", assetClasses)}"));

    [Description("Assesses investment risk based on a market analysis result.")]
    static async Task<string> InvokeRiskAssessmentAgent([Description("Market analysis text the risk assessment is based on")] string marketData) =>
        JsonSerializer.Serialize(await Resilience.InvokeSpoke("risk_assessment", $"Assess risk based on: {marketData}"));

    [Description("Checks a proposed investment against concentration and suitability rules.")]
    static async Task<string> InvokeComplianceAgent([Description("The investment proposal to check")] string proposal) =>
        JsonSerializer.Serialize(await Resilience.InvokeSpoke("compliance_check", $"Check this proposal: {proposal}"));

    public static async Task<string> HubAndSpoke(string query)
    {
        AIAgent hub = Spokes.MakeAgent(
            "research_orchestrator",
            HubInstructions,
            [AIFunctionFactory.Create(InvokeMarketAnalysisAgent), AIFunctionFactory.Create(InvokeRiskAssessmentAgent), AIFunctionFactory.Create(InvokeComplianceAgent)]);
        return (await hub.RunAsync(query)).Text;
    }
}
