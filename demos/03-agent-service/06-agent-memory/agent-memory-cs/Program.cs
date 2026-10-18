using System.ClientModel;
using System.ClientModel.Primitives;
using System.Text.Json;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Projects.Memory;
using Azure.Identity;
using OpenAI.Responses;

DotNetEnv.Env.TraversePath().Load();

const string store = "m3-memory-store";
const string agentName = "m3-memory-agent";
string model = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
string embeddingModel = Environment.GetEnvironmentVariable("EMBEDDING_MODEL_DEPLOYMENT_NAME")!;
Uri endpoint = new(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!);
DefaultAzureCredential credential = new();

AIProjectClient project = new(endpoint, credential);
AIProjectMemoryStores memoryStores = project.MemoryStores;

void Show(IEnumerable<MemorySearchItem> memories)
{
    foreach (MemorySearchItem entry in memories)
        Console.WriteLine($"  [{KindOf(entry.MemoryItem)}] {Trim(entry.MemoryItem.Content)}");
}

static string KindOf(MemoryItem item) => item switch
{
    Azure.AI.Projects.Memory.UserProfileMemoryItem => "user_profile",
    Azure.AI.Projects.Memory.ChatSummaryMemoryItem => "chat_summary",
    _ => "procedural",
};

static string Trim(string text) => text.Length > 150 ? text[..150] : text;

ProjectResponsesClient ClientFor(string user, AgentReference agent, ProjectConversation conversation)
{
    ProjectOpenAIClientOptions options = new();
    options.AddPolicy(new HeaderPolicy("x-memory-user-id", user), PipelinePosition.PerCall);
    return new ProjectOpenAIClient(endpoint, credential, options).GetProjectResponsesClientForAgent(agent, conversation.Id);
}

Console.WriteLine("== Step 1: create a memory store");
string definition = JsonSerializer.Serialize(new
{
    name = store,
    description = "Module 3 memory demo",
    definition = new
    {
        kind = "default",
        chat_model = model,
        embedding_model = embeddingModel,
        options = new
        {
            user_profile_enabled = true,
            chat_summary_enabled = true,
            procedural_memory_enabled = true,
            default_ttl_seconds = 86400,
            user_profile_details = "Avoid irrelevant or sensitive data, such as age, financials, precise location and credentials",
        },
    },
});
await memoryStores.CreateMemoryStoreAsync(BinaryContent.Create(BinaryData.FromString(definition)));
Console.WriteLine($"Created {store}");

Console.WriteLine("== Step 2: write memories from a conversation");
MemoryUpdateOptions update = new("alice")
{
    UpdateDelay = 0,
    Items =
    {
        ResponseItem.CreateUserMessageItem("I prefer dark roast coffee and I am allergic to peanuts."),
        ResponseItem.CreateUserMessageItem("Please publish the release notes for version 2.4."),
        ResponseItem.CreateAssistantMessageItem("I posted them straight to the wiki."),
        ResponseItem.CreateUserMessageItem(
            "No, that is wrong. The process for publishing release notes is: first run the changelog script, " +
            "then get approval from the release manager in the review channel, and only then publish to the wiki."),
        ResponseItem.CreateAssistantMessageItem("Understood. I will run the changelog script, request approval from the release manager and publish only after approval."),
        ResponseItem.CreateUserMessageItem("Right. Do the same for version 2.5."),
        ResponseItem.CreateAssistantMessageItem("Running the changelog script for 2.5 now, then asking the release manager for approval before publishing."),
        ResponseItem.CreateUserMessageItem("Good, that is how we always do it."),
    },
};
MemoryUpdateResult result = (await memoryStores.UpdateMemoriesAsync(store, update)).Value;
while (result.Status is MemoryStoreUpdateStatus.Queued or MemoryStoreUpdateStatus.InProgress)
{
    await Task.Delay(2000);
    result = (await memoryStores.GetUpdateResultAsync(store, result.UpdateId)).Value;
}
foreach (MemoryOperation operation in result.Details!.MemoryOperations)
    Console.WriteLine($"  {operation.Kind} [{KindOf(operation.MemoryItem)}] {Trim(operation.MemoryItem.Content)}");

Console.WriteLine("== Step 3: search static and contextual memories");
Console.WriteLine("static memories for alice:");
Show((await memoryStores.SearchMemoriesAsync(store, new MemorySearchOptions("alice"))).Value.Memories);
Console.WriteLine("contextual memories for 'Publish the release notes for 2.6':");
Show((await memoryStores.SearchMemoriesAsync(store, new MemorySearchOptions("alice")
{
    Items = { ResponseItem.CreateUserMessageItem("Publish the release notes for 2.6") },
    ResultOptions = new MemorySearchResultOptions { MaxMemories = 5 },
})).Value.Memories);
MemorySearchOptions bobSearch = new("bob") { Items = { ResponseItem.CreateUserMessageItem("coffee") } };
Console.WriteLine($"memories visible to bob: {(await memoryStores.SearchMemoriesAsync(store, bobSearch)).Value.Memories.Count}");

Console.WriteLine("== Step 4: attach the memory search tool to an agent");
ProjectsAgentVersion agent = (await project.AgentAdministrationClient.CreateAgentVersionAsync(
    agentName: agentName,
    options: new(new DeclarativeAgentDefinition(model)
    {
        Instructions = "You are a helpful assistant. Use what you remember about the user and say when you remember nothing.",
        Tools = { new MemorySearchPreviewTool(store, "{{$userId}}") { UpdateDelayInSecs = 1 } },
    }))).Value;
Console.WriteLine($"Created {agent.Name}:{agent.Version}");
AgentReference reference = new(agent.Name, agent.Version);
ProjectConversationsClient conversations = project.ProjectOpenAIClient.GetProjectConversationsClient();

async Task<string> Ask(string user, ProjectConversation conversation, string text) =>
    (await ClientFor(user, reference, conversation).CreateResponseAsync(text)).Value.GetOutputText();

ProjectConversation first = (await conversations.CreateProjectConversationAsync()).Value;
Console.WriteLine("carol, conversation 1: I prefer oat milk in my flat white");
Console.WriteLine($"agent: {await Ask("carol", first, "I prefer oat milk in my flat white.")}");
Console.WriteLine("waiting for memories to be written");
await Task.Delay(TimeSpan.FromSeconds(65));

Console.WriteLine("== Step 5: recall in a new conversation and prove scope isolation");
foreach (string user in new[] { "carol", "dave" })
{
    ProjectConversation fresh = (await conversations.CreateProjectConversationAsync()).Value;
    Console.WriteLine($"{user}, new conversation: Please order my usual coffee");
    Console.WriteLine($"agent: {await Ask(user, fresh, "Please order my usual coffee.")}");
}

Console.WriteLine("== Step 6: remember and forget on command");
ResponsesClient direct = project.ProjectOpenAIClient.GetProjectResponsesClient();
foreach (string text in new[] { "Remember that my preferred seat is aisle.", "Forget my preferred seat." })
{
    string body = JsonSerializer.Serialize(new
    {
        model,
        input = new[] { new { type = "message", role = "user", content = text } },
        tools = new[] { new { type = "memory_search_preview", memory_store_name = store, scope = "erin" } },
    });
    ClientResult raw = await direct.CreateResponseAsync(BinaryContent.Create(BinaryData.FromString(body)));
    using JsonDocument response = JsonDocument.Parse(raw.GetRawResponse().Content);
    foreach (JsonElement item in response.RootElement.GetProperty("output").EnumerateArray())
        if (item.GetProperty("type").GetString() == "memory_command_preview_call")
            Console.WriteLine($"{text} -> {item.GetProperty("arguments")} ({item.GetProperty("status")})");
}

Console.WriteLine("== Cleanup");
foreach (string scope in new[] { "alice", "bob", "carol", "dave", "erin" })
    await memoryStores.DeleteScopeAsync(store, scope);
await project.AgentAdministrationClient.DeleteAgentVersionAsync(agentName: agent.Name, agentVersion: agent.Version);
await memoryStores.DeleteMemoryStoreAsync(store);
Console.WriteLine("Deleted scopes, agent version and memory store");

class HeaderPolicy(string name, string value) : PipelinePolicy
{
    public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        message.Request.Headers.Set(name, value);
        ProcessNext(message, pipeline, currentIndex);
    }

    public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
    {
        message.Request.Headers.Set(name, value);
        await ProcessNextAsync(message, pipeline, currentIndex);
    }
}
