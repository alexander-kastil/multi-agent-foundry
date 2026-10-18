using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;

static class AgentIdentity
{
    public static async Task Run()
    {
        AIProjectClient project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
        foreach (string name in new[] { "m8h-idn-orders", "m8h-idn-hr" })
        {
            DeclarativeAgentDefinition definition = new(Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!)
            {
                Instructions = "You answer in one sentence.",
            };
            await project.AgentAdministrationClient.CreateAgentVersionAsync(name, new ProjectsAgentVersionCreationOptions(definition));

            ClientResult agent = await project.AgentAdministrationClient.GetAgentAsync(name, new RequestOptions());
            JsonNode body = JsonNode.Parse(agent.GetRawResponse().Content)!;
            Console.WriteLine(name);
            Console.WriteLine($"  agent identity principal_id: {body["instance_identity"]!["principal_id"]}");
            Console.WriteLine($"  agent identity client_id:    {body["instance_identity"]!["client_id"]}");
            Console.WriteLine($"  blueprint reference:         {body["blueprint_reference"]!["blueprint_id"]}");
            Console.WriteLine($"  inbound authorization:       {body["agent_endpoint"]!["authorization_schemes"]![0]!["type"]}");
        }
    }
}
