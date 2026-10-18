using System.Globalization;
using System.ClientModel.Primitives;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Declarative;
using Microsoft.Extensions.Configuration;
using OpenAI.Conversations;
using OpenAI.Responses;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
Console.OutputEncoding = System.Text.Encoding.UTF8;
DotNetEnv.Env.TraversePath().Load();

Uri endpoint = new(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
string model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
DefaultAzureCredential credential = new();
AIProjectClient project = new(endpoint, credential);
string[] agentNames = ["m35h-support-router", "m35h-classifier", "m35h-billing", "m35h-technical"];
string[] messages = args.Skip(1).ToArray();

switch (args.FirstOrDefault())
{
    case "expense":
        await RunLocal("workflows/expense-triage.yaml",
        [
            "420,travel",
            "5000,office",
        ]);
        break;
    case "router":
        await RunLocal("workflows/support-router.yaml", DefaultMessages(messages));
        break;
    case "publish":
        await Publish();
        break;
    case "foundry":
        await RunInFoundry(DefaultMessages(messages));
        break;
    case "export":
        Export(messages.FirstOrDefault() ?? "1");
        break;
    case "migrated":
        await RunLocal("migration/exported.workflow.yaml", DefaultMessages(messages));
        break;
    case "cleanup":
        await Cleanup();
        break;
    default:
        Console.WriteLine("Usage: dotnet run -- expense | publish | router | foundry | export [version] | migrated | cleanup");
        break;
}

string[] DefaultMessages(string[] given) =>
    given.Length > 0 ? given : ["I was charged twice this month.", "The app crashes when I scan a receipt.", "Do you have a partner program?"];

async Task RunLocal(string path, string[] inputs)
{
    AzureAgentProvider agentProvider = new(endpoint, credential);
    DeclarativeWorkflowOptions options = new(agentProvider) { Configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build() };
    foreach (string input in inputs)
    {
        Workflow workflow = DeclarativeWorkflowBuilder.Build<string>(path, options);
        List<string> outputs = [];
        StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, input);
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentResponseEvent response)
                outputs.Add(response.Response.Text);
            else if (evt is WorkflowErrorEvent error)
                Console.WriteLine($"Error: {error.Data}");
        }
        Console.WriteLine($"{input} -> [{string.Join(", ", outputs.Select(output => $"'{output}'"))}]");
    }
}

async Task Publish()
{
    AgentAdministrationClient agents = project.AgentAdministrationClient;
    ResponseTextOptions classifierFormat = new()
    {
        TextFormat = ResponseTextFormat.CreateJsonSchemaFormat(
            "classification",
            BinaryData.FromString("""
                {
                  "type": "object",
                  "properties": { "category": { "type": "string", "enum": ["billing", "technical", "other"] } },
                  "required": ["category"],
                  "additionalProperties": false
                }
                """),
            jsonSchemaIsStrict: true),
    };
    await agents.CreateAgentVersionAsync("m35h-classifier", new ProjectsAgentVersionCreationOptions(new DeclarativeAgentDefinition(model)
    {
        Instructions = "Classify the customer message as billing, technical or other.",
        TextOptions = classifierFormat,
    }));
    await agents.CreateAgentVersionAsync("m35h-billing", new ProjectsAgentVersionCreationOptions(new DeclarativeAgentDefinition(model) { Instructions = "You answer billing questions in two sentences." }));
    await agents.CreateAgentVersionAsync("m35h-technical", new ProjectsAgentVersionCreationOptions(new DeclarativeAgentDefinition(model) { Instructions = "You answer technical questions in two sentences." }));
    string workflowYaml = File.ReadAllText("migration/portal-workflow.yaml");
    ProjectsAgentVersion version = (await agents.CreateAgentVersionAsync("m35h-support-router", new ProjectsAgentVersionCreationOptions(WorkflowAgentDefinition.FromYaml(workflowYaml)))).Value;
    Console.WriteLine($"published {version.Name} version {version.Version}");
}

async Task RunInFoundry(string[] inputs)
{
    ProjectConversationsClient conversations = project.ProjectOpenAIClient.GetProjectConversationsClient();
    foreach (string input in inputs)
    {
        ConversationResource conversation = (await conversations.CreateProjectConversationAsync()).Value;
        ProjectResponsesClient client = project.ProjectOpenAIClient.GetProjectResponsesClientForAgent(new AgentReference("m35h-support-router"), conversation.Id);
        ResponseResult response = (await client.CreateResponseAsync(input)).Value;
        Console.WriteLine($"{input} -> {response.GetOutputText()}");
        await conversations.DeleteConversationAsync(conversation.Id);
    }
}

void Export(string versionNumber)
{
    ProjectsAgentVersion version = project.AgentAdministrationClient.GetAgentVersion("m35h-support-router", versionNumber).Value;
    string yaml = JsonDocument.Parse(ModelReaderWriter.Write(version.Definition)).RootElement.GetProperty("workflow").GetString()!;
    File.WriteAllText("migration/exported.workflow.yaml", yaml);
    Console.WriteLine($"exported {version.Name} version {version.Version}, {yaml.Split('\n').Length} lines");
}

async Task Cleanup()
{
    foreach (string name in agentNames)
    {
        await project.AgentAdministrationClient.DeleteAgentAsync(name);
        Console.WriteLine($"deleted {name}");
    }
}
