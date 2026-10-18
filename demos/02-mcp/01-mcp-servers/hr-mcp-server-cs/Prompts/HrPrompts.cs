using System.ComponentModel;
using ModelContextProtocol.Server;

namespace HrMcpServer.Prompts;

[McpServerPromptType]
public class HrPrompts
{
    [McpServerPrompt(Name = "onboarding_plan")]
    [Description("Drafts a first-week onboarding plan for a new employee.")]
    public static string OnboardingPlan(
        [Description("Role of the new employee")] string role,
        [Description("Name of the buddy who guides the first week")] string buddy) =>
        $"Draft a first-week onboarding plan for a new {role}. Pair them with {buddy}. Use list_employees to find colleagues they should meet and read hr://policy/remote-work for scheduling rules.";
}
