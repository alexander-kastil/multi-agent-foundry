using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

static class Spokes
{
    static readonly AIProjectClient Project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
    static readonly string Model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;

    static readonly Dictionary<string, string> Instructions = new()
    {
        ["market_analysis"] = "You analyze equity market conditions and return a market report of at most four sentences.",
        ["risk_assessment"] = "You assess investment risk from the data you are given and return a risk score from 1 to 10 plus two reasons.",
        ["compliance_check"] = "You check a proposed investment against concentration and suitability rules and return PASS or FAIL plus one reason.",
        ["esg_evaluation"] = "You evaluate environmental, social and governance factors and return a one sentence ESG view.",
    };

    public static AIAgent MakeAgent(string name, string instructions, IList<AITool>? tools = null) =>
        Project.AsAIAgent(model: Model, instructions: instructions, name: name, tools: tools);

    public static Dictionary<string, AIAgent> MakeSpokes() =>
        Instructions.ToDictionary(pair => pair.Key, pair => MakeAgent(pair.Key, pair.Value));
}
