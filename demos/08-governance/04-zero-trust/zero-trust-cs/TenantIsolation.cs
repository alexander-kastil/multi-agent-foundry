using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.IdentityModel.Tokens;
using OpenAI.Responses;

static class TenantIsolation
{
    static readonly JsonSerializerOptions Snake = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public static async Task Run()
    {
        RsaSecurityKey signingKey = TenantContext.NewSigningKey();
        RsaSecurityKey otherKey = TenantContext.NewSigningKey();

        PartitionedStore store = new();
        CodeReviewRepository repository = new(store);

        var seed = new Dictionary<string, (string Id, string File, string[] Findings)[]>
        {
            ["tenant-a"] = [("rev-a-001", "billing.py", ["hardcoded api key"]), ("rev-a-002", "auth.py", [])],
            ["tenant-b"] = [("rev-b-001", "payroll.py", ["sql injection in salary export"])],
        };
        foreach ((string tenant, var reviews) in seed)
        {
            TenantContext.CurrentTenant = tenant;
            foreach (var (id, file, findings) in reviews) repository.SaveReview(id, file, findings);
        }

        string forged = TenantContext.IssueToken(otherKey, "tenant-b");
        try
        {
            await TenantContext.ExtractTenantFromToken($"Bearer {forged}", signingKey);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Forged token for tenant-b rejected: {e.GetType().Name}");
        }

        string token = TenantContext.IssueToken(signingKey, "tenant-a");
        string tenantId = await TenantContext.ExtractTenantFromToken($"Bearer {token}", signingKey);
        TenantContext.CurrentTenant = tenantId;
        Console.WriteLine($"Request context established for {tenantId}\n");

        AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());

        ResponseTool listReviews = ResponseTool.CreateFunctionTool(
            "list_reviews",
            BinaryData.FromString("""{"type":"object","properties":{},"required":[],"additionalProperties":false}"""),
            strictModeEnabled: true,
            functionDescription: "List the code reviews of the current tenant.");
        ResponseTool getReview = ResponseTool.CreateFunctionTool(
            "get_review",
            BinaryData.FromString("""
            {"type":"object","properties":{"review_id":{"type":"string","description":"Review id, e.g. rev-a-001."}},"required":["review_id"],"additionalProperties":false}
            """),
            strictModeEnabled: true,
            functionDescription: "Fetch one code review by id from the current tenant.");

        DeclarativeAgentDefinition definition = new(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
        {
            Instructions = "You answer questions about code reviews using only the tools. Report exactly what the tools return.",
            Tools = { listReviews, getReview },
        };
        ProjectsAgentVersion agent = await project.AgentAdministrationClient.CreateAgentVersionAsync("m8h-zt-review-assistant", new ProjectsAgentVersionCreationOptions(definition));
        ProjectConversation conversation = await project.ProjectOpenAIClient.GetProjectConversationsClient().CreateProjectConversationAsync();
        ProjectResponsesClient responses = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference(agent.Name, agent.Version), conversation);

        string prompt = "List my reviews, then fetch review rev-b-001 and tell me what it contains.";
        Console.WriteLine($"USER: {prompt}");
        ResponseResult response = await responses.CreateResponseAsync(prompt);

        while (true)
        {
            List<ResponseItem> outputs = [];
            foreach (FunctionCallResponseItem call in response.OutputItems.OfType<FunctionCallResponseItem>())
            {
                JsonNode args = JsonNode.Parse(call.FunctionArguments)!;
                object? result = call.FunctionName == "list_reviews" ? repository.ListReviews() : repository.GetReview((string)args["review_id"]!);
                string json = JsonSerializer.Serialize(result, Snake);
                Console.WriteLine($"  [tool call] {call.FunctionName}({args.ToJsonString()}) -> {json}");
                outputs.Add(ResponseItem.CreateFunctionCallOutputItem(call.CallId, json));
            }
            if (outputs.Count == 0) break;
            response = await responses.CreateResponseAsync(outputs);
        }

        Console.WriteLine($"\nAGENT: {response.GetOutputText()}");

        await project.ProjectOpenAIClient.GetProjectConversationsClient().DeleteConversationAsync(conversation.Id);
        await project.AgentAdministrationClient.DeleteAgentVersionAsync(agent.Name, agent.Version);
    }
}
