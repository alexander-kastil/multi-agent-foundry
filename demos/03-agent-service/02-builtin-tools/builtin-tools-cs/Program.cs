using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using OpenAI.Containers;
using OpenAI.Files;
using OpenAI.Responses;
using OpenAI.VectorStores;

DotNetEnv.Env.TraversePath().Load();

string model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
string assets = Path.Combine(AppContext.BaseDirectory, "assets");
Directory.CreateDirectory("output");

AIProjectClient project = new(
    endpoint: new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!),
    tokenProvider: new DefaultAzureCredential());

List<ProjectsAgentVersion> agents = [];

ProjectsAgentVersion MakeAgent(string name, string instructions, params ResponseTool[] tools)
{
    DeclarativeAgentDefinition definition = new(model) { Instructions = instructions };
    foreach (ResponseTool tool in tools) definition.Tools.Add(tool);
    ProjectsAgentVersion agent = project.AgentAdministrationClient.CreateAgentVersion(name, new ProjectsAgentVersionCreationOptions(definition));
    agents.Add(agent);
    Console.WriteLine($"Created {agent.Name}:{agent.Version}");
    return agent;
}

ResponseResult Ask(ProjectsAgentVersion agent, string text) =>
    project.ProjectOpenAIClient
        .GetProjectResponsesClientForAgent(new AgentReference(agent.Name, agent.Version))
        .CreateResponse(text);

string ToolCalls(ResponseResult response) =>
    string.Join(", ", response.OutputItems.Select(i => i.GetType().Name).Where(n => n.Contains("Call")));

OpenAIFileClient files = project.ProjectOpenAIClient.GetOpenAIFileClient();
VectorStoreClient stores = project.ProjectOpenAIClient.GetVectorStoreClient();

Console.WriteLine("== Step 1: Code Interpreter analyses a CSV and returns a chart");
OpenAIFile csv = files.UploadFile(Path.Combine(assets, "quarterly_results.csv"), FileUploadPurpose.Assistants);
ProjectsAgentVersion analyst = MakeAgent(
    "m3-analyst",
    "You analyse uploaded data with Python. Always save charts as PNG files.",
    ResponseTool.CreateCodeInterpreterTool(
        new CodeInterpreterToolContainer(
            CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration(fileIds: [csv.Id]))));
ResponseResult chart = Ask(analyst, "Create a bar chart of operating_profit by company for the TRANSPORTATION sector in the uploaded CSV and give me the PNG.");
Console.WriteLine($"tool calls: {ToolCalls(chart)}");
ContainerClient containers = project.ProjectOpenAIClient.GetContainerClient();
foreach (ResponseItem item in chart.OutputItems)
{
    if (item is not MessageResponseItem message) continue;
    foreach (ResponseContentPart part in message.Content)
    foreach (ResponseMessageAnnotation annotation in part.OutputTextAnnotations)
    {
        if (annotation is not ContainerFileCitationMessageAnnotation citation) continue;
        BinaryData data = containers.DownloadContainerFile(citation.ContainerId, citation.FileId);
        string target = Path.Combine("output", Path.GetFileName(citation.Filename));
        File.WriteAllBytes(target, data.ToArray());
        Console.WriteLine($"saved {target} ({data.ToArray().Length} bytes)");
    }
}

Console.WriteLine("== Step 2: File Search answers from an uploaded document");
OpenAIFile doc = files.UploadFile(Path.Combine(assets, "product-info.md"), FileUploadPurpose.Assistants);
VectorStore store = stores.CreateVectorStore(new VectorStoreCreationOptions { Name = "m3-product-docs", FileIds = { doc.Id } });
Console.WriteLine($"vector store {store.Id} ready");
ProjectsAgentVersion support = MakeAgent(
    "m3-doc-support",
    "Answer only from the product documents. Say when the documents do not contain the answer.",
    ResponseTool.CreateFileSearchTool(vectorStoreIds: [store.Id]));
foreach (string question in new[] { "How long is the warranty and what does it cover?", "What is the battery capacity in mAh?" })
{
    ResponseResult answer = Ask(support, question);
    Console.WriteLine($"user: {question}\ntool calls: {ToolCalls(answer)}\nagent: {answer.GetOutputText()}");
}

Console.WriteLine("== Step 3: Web Search grounds an answer in current pages");
ProjectsAgentVersion researcher = MakeAgent(
    "m3-web-researcher",
    "Answer with current facts from the web and cite your sources.",
    ResponseTool.CreateWebSearchTool());
ResponseResult news = Ask(researcher, "What is the latest stable version of the Azure.AI.Projects package on NuGet?");
Console.WriteLine($"tool calls: {ToolCalls(news)}\nagent: {news.GetOutputText()}");

Console.WriteLine("== Cleanup");
foreach (ProjectsAgentVersion agent in agents)
{
    project.AgentAdministrationClient.DeleteAgentVersion(agent.Name, agent.Version);
    Console.WriteLine($"deleted {agent.Name}:{agent.Version}");
}
stores.DeleteVectorStore(store.Id);
files.DeleteFile(csv.Id);
files.DeleteFile(doc.Id);
Console.WriteLine("deleted vector store and files");
