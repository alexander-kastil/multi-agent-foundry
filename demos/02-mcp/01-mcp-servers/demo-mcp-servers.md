# Build and Test an HR MCP Server in C# and Python

An HR directory is useful to an agent only if the agent can discover and call it. You will run the same server in C# and in Python, inspect its tools, resources and prompts without a model, and then let a Foundry-hosted model call it through Agent Framework.

## Prerequisites

- .NET 10 SDK, Python 3.12, Node.js 20 or later (for `npx`).
- `az login` on a subscription that can reach your Foundry project, and a `gpt-5.4` deployment.
- Ports 47002 (C# server) and 47003 (Python server) free.

## Run it

```powershell
cd demos/02-mcp/01-mcp-servers/hr-mcp-server-cs
dotnet run --urls http://localhost:47002
```

In a second terminal:

```powershell
cd demos/02-mcp/01-mcp-servers/hr-mcp-server-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
python server.py --http
```

The Python server listens on `http://127.0.0.1:47003/mcp` and the C# server on `http://localhost:47002/mcp`.

## Step 1: List what the C# server exposes

The MCP Inspector CLI is a generic client. It connects, asks the server for its tools and prints the JSON the server returned, so you see exactly what a model would see.

```powershell
npx @modelcontextprotocol/inspector --cli http://localhost:47002/mcp --transport http --method tools/list
```

Each tool carries a description, an `inputSchema` and annotations. The annotations come from the `ReadOnly` and `Destructive` flags in the C# attributes.

Expected result (names and annotations only):

```text
add_employee {'destructiveHint': False}
search_employees {'readOnlyHint': True}
remove_employee {'destructiveHint': True}
list_employees {'readOnlyHint': True}
```

## Step 2: Read the tool definitions

Open `hr-mcp-server-cs/Tools/HrTools.cs`. A class marked `[McpServerToolType]` is scanned, and each `[McpServerTool]` method becomes a tool.

```csharp
[McpServerToolType]
public class HrTools(EmployeeStore store)
{
    [McpServerTool(Name = "search_employees", ReadOnly = true)]
    [Description("Finds employees whose name, email, role or skills contain the search term.")]
    public IReadOnlyList<Employee> SearchEmployees(
        [Description("Text to look for, for example a skill such as baking")] string searchTerm) => store.Search(searchTerm);
}
```

`EmployeeStore` arrives through the constructor because `Program.cs` registers it as a singleton. Call the tool with the Inspector to confirm the argument name `searchTerm` comes from the C# parameter.

```powershell
npx @modelcontextprotocol/inspector --cli http://localhost:47002/mcp --transport http --method tools/call --tool-name search_employees --tool-arg searchTerm=baking
```

Expected result:

```text
{
  "content": [
    {
      "type": "text",
      "text": "[{\"firstName\":\"Marie\",\"lastName\":\"Dubois\",\"email\":\"marie.dubois@restaurant.com\",\"currentRole\":\"Pastry Chef\",\"skills\":[\"Dessert Creation\",\"Baking\"],\"spokenLanguages\":[\"French\",\"English\",\"Italian\"]}]"
    }
  ]
}
```

## Step 3: Add resources and prompts

Tools are for the model. Resources and prompts give the application and the user their own entry points. `Resources/HrResources.cs` publishes a fixed policy document and a URI template, and `Prompts/HrPrompts.cs` publishes a prompt.

```csharp
[McpServerResource(UriTemplate = "hr://employees/{email}", Name = "employee_profile", MimeType = "application/json")]
[Description("The profile of one employee, addressed by email.")]
public string EmployeeProfile(string email) =>
    JsonSerializer.Serialize(store.Find(email) ?? throw new McpException($"Employee {email} not found"));
```

`Program.cs` turns them on with `WithResourcesFromAssembly()` and `WithPromptsFromAssembly()`. Ask the server for the template and render the prompt.

```powershell
npx @modelcontextprotocol/inspector --cli http://localhost:47002/mcp --transport http --method resources/templates/list
npx @modelcontextprotocol/inspector --cli http://localhost:47002/mcp --transport http --method prompts/get --prompt-name onboarding_plan --prompt-args role="Line Cook" buddy=Carlos
```

Expected result:

```text
{
  "resourceTemplates": [
    {
      "name": "employee_profile",
      "uriTemplate": "hr://employees/{email}",
      "description": "The profile of one employee, addressed by email.",
      "mimeType": "application/json"
    }
  ]
}
{
  "description": "Drafts a first-week onboarding plan for a new employee.",
  "messages": [
    {
      "role": "user",
      "content": {
        "type": "text",
        "text": "Draft a first-week onboarding plan for a new Line Cook. Pair them with Carlos. Use list_employees to find colleagues they should meet and read hr://policy/remote-work for scheduling rules."
      }
    }
  ]
}
```

## Step 4: Run the same server in Python

`hr-mcp-server-py/server.py` declares the same surface with decorators. `mcp` 2.x renamed `FastMCP` to `MCPServer`.

```python
from mcp.server.mcpserver import MCPServer

mcp = MCPServer("hr-mcp-server-py", instructions="HR directory for a restaurant team.")


@mcp.tool(name="search_employees", annotations=ToolAnnotations(read_only_hint=True))
def search_employees(search_term: str) -> list[dict]:
    """Finds employees whose name, email, role or skills contain the search term."""
    return [e.to_dict() for e in store.search(search_term)]
```

`client.py` uses the SDK client to list every primitive, call a tool, read a resource and render the prompt. Run it against the HTTP server you started.

```powershell
python client.py http://127.0.0.1:47003/mcp
```

Expected result (trimmed):

```text
tools: ['list_employees', 'search_employees', 'add_employee', 'remove_employee']
resources: ['hr://policy/remote-work']
templates: ['hr://employees/{email}']
prompts: ['onboarding_plan']
search_employees: {
  "first_name": "Marie",
  "last_name": "Dubois",
  "email": "marie.dubois@restaurant.com",
  ...
policy: # Remote work policy
profile: {
  "first_name": "Luca",
  ...
prompt: Draft a first-week onboarding plan for a new Line Cook. Pair them with Carlos. Use list_employees to find colleagues they should meet and read hr://policy/remote-work for scheduling rules.
```

The Python tool takes `search_term` where the C# tool takes `searchTerm`. Each SDK follows its language convention, so a generic client always reads the schema.

![The search_employees tool in C# and Python mapped line by line to its tools/list entry, with the argument names searchTerm and search_term highlighted](../pptx/images/01-01-tool-schemas.svg)

## Step 5: Switch between stdio and HTTP

Both servers start in a different transport depending on the arguments. The C# `Program.cs` branches on `--stdio`, and the Python `server.py` branches on `--http`.

```csharp
if (args.Contains("--stdio"))
{
    var host = Host.CreateApplicationBuilder(args);
    host.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
    host.Services.AddSingleton<EmployeeStore>();
    host.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly().WithResourcesFromAssembly().WithPromptsFromAssembly();
    await host.Build().RunAsync();
    return;
}
```

With stdio the client launches the server itself. Run the Python client without a URL and it starts `server.py` as a child process, and let the Inspector start the C# server the same way. The logging goes to standard error because standard output carries the protocol.

```powershell
python client.py
npx @modelcontextprotocol/inspector --cli dotnet run --project ../hr-mcp-server-cs --no-build -- --stdio --method prompts/list
```

Expected result: the Python client prints the same lines as in step 4. The Inspector prints the `onboarding_plan` prompt with its two required arguments, `role` and `buddy`, and the server log lines go to standard error.

## Step 6: Let a Foundry model call the server

`hr-agent-py` connects Agent Framework to the Python server. It needs its own environment because `agent-framework-core` 1.22.0 requires `mcp` below 2.

```powershell
cd ../hr-agent-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
copy .env.example .env
python agent.py
```

Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env` first. The script connects `MCPStreamableHTTPTool` to the Python server and asks a question that needs the directory.

![agent.py lists the HR server's tools, passes their schemas to the Foundry model, runs the search_employees call the model asks for, and returns the answer about Marie Dubois](../pptx/images/01-02-agent-tool-call.svg)

```python
hr = MCPStreamableHTTPTool(name="hr", url=SERVER_URL, approval_mode="never_require")
async with Agent(client=client, instructions="You are an HR assistant. Use the hr tools for every question about staff.", tools=[hr]) as agent:
    result = await agent.run("Who on the team has baking skills, and which languages do they speak?")
    print(result.text)
```

Expected result (the wording of a model answer varies):

```text
Marie Dubois has baking skills. She speaks French, English, and Italian.
```

To use the C# server instead, set `MCP_SERVER_URL=http://127.0.0.1:47002/mcp` and run `python agent.py` again. The model reads the C# schema and sends `searchTerm`.

## Verify

You are done when all of these hold:

- `tools/list` returns four tools on both servers, with `readOnlyHint` on the two query tools.
- The Python client prints tools, resources, the template and the prompt.
- The agent names Marie Dubois and her three languages, which proves the model called a tool, because the data exists only in the server.

## Cleanup

Stop both servers with Ctrl+C. Nothing was created in Azure.

```powershell
Get-NetTCPConnection -LocalPort 47002,47003 -State Listen -ErrorAction SilentlyContinue
```

The command prints nothing once both ports are free.
