# Publish Tools Once with a Foundry Toolbox

Three agents that need the same documentation search and snippet store should not each carry the tool definitions, the key and the approval rules. You will bundle a Microsoft Learn search tool and the Functions snippets server from the previous demo into one toolbox, read it through its managed MCP endpoint, give it to an Agent Framework agent in Python and .NET, and release a new version without changing the agent.

## Prerequisites

- The function app and the project connection `m2-func-mcp-key` from the previous demo. Any other remote MCP server and connection also works if you change the two `SNIPPETS_` variables.
- `az login`, the Foundry User role on the project and a `gpt-5.4` deployment.
- Python 3.12, .NET 10 SDK.
- Port 47020 free for step 5.

## Run it

```powershell
cd demos/02-mcp/06-foundry-toolbox/toolbox-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
copy .env.example .env
```

Set `PROJECT_ENDPOINT`, `MODEL_DEPLOYMENT_NAME`, `TOOLBOX_NAME`, `SNIPPETS_MCP_URL` and `SNIPPETS_CONNECTION_NAME` in `.env`. The requirements pin `azure-ai-projects==2.7.0` because `agent-framework-foundry` 1.15.0 requires a release below 2.8. They also pin `agent-framework-foundry-hosting`, a beta package that provides `FoundryToolbox`, and `mcp==1.30.0` because Agent Framework 1.22.0 cannot read the initialize result of `mcp` 2.x.

## Step 1: Create the toolbox

`create_toolbox.py` calls `create_version` with two MCP tools. The Learn server needs no credentials. The snippets server references the connection that holds the function key.

```python
version = project.toolboxes.create_version(
    name=os.environ["TOOLBOX_NAME"],
    description="Microsoft Learn search plus the snippets server hosted on Azure Functions",
    tools=[
        MCPToolboxTool(server_label="mslearn", server_url="https://learn.microsoft.com/api/mcp", description="Search and read Microsoft Learn documentation", require_approval="never"),
        MCPToolboxTool(server_label="snippets", server_url=os.environ["SNIPPETS_MCP_URL"], project_connection_id=os.environ["SNIPPETS_CONNECTION_NAME"], description="Save and read named code snippets", require_approval="never"),
    ],
)
```

```powershell
python create_toolbox.py
```

Expected result:

```text
toolbox m2-docs-and-snippets: created version 1, default version 1
consumer endpoint: https://<resource>.services.ai.azure.com/api/projects/<project>/toolboxes/m2-docs-and-snippets/mcp?api-version=v1
version endpoint:  https://<resource>.services.ai.azure.com/api/projects/<project>/toolboxes/m2-docs-and-snippets/versions/1/mcp?api-version=v1
```

The first version became the default automatically.

## Step 2: Read the tools through the managed endpoint

`list_tools.py` is a plain MCP client. It gets an Entra token for `https://ai.azure.com/.default`, connects to the toolbox endpoint, lists the tools and calls one.

```python
token = DefaultAzureCredential().get_token("https://ai.azure.com/.default").token
async with streamablehttp_client(URL, headers={"Authorization": f"Bearer {token}"}) as (read, write, _):
    async with ClientSession(read, write) as session:
        await session.initialize()
        tools = await session.list_tools()
```

```powershell
python list_tools.py
```

Expected result:

```text
pro-code-agents/toolboxes/m2-docs-and-snippets/mcp?api-version=v1
  mslearn___microsoft_code_sample_search: Search for code snippets and examples in official Microsoft Learn docu
  mslearn___microsoft_docs_fetch: Fetch and convert a Microsoft Learn documentation webpage to markdown 
  mslearn___microsoft_docs_search: Search official Microsoft/Azure documentation to find the most relevan
  snippets___get_snippet: Retrieves a snippet by name from blob storage.
  snippets___hello_mcp: Says hello from an MCP server hosted on Azure Functions.
  snippets___save_snippet: Saves a snippet under a name into blob storage.
snippets___hello_mcp -> Hello I am MCPTool!
```

Both servers appear behind one endpoint. Each tool name starts with its server label and three underscores. The call reached the Functions app through the connection, so your script never saw the function key.

## Step 3: Give the toolbox to an agent

`agent_toolbox.py` builds an Agent Framework agent whose only tool is the toolbox. `FoundryToolbox` handles the token and the preview header.

```python
toolbox = FoundryToolbox(credential, url=TOOLBOX_URL)
async with Agent(
    client=client,
    instructions="Answer from Microsoft Learn and keep the snippets collection current. Keep answers under 60 words.",
    tools=[toolbox],
) as agent:
    first = await agent.run("Look up the CLI command that sets the project endpoint for azd ai, then save it as a snippet named azd-project.")
    second = await agent.run("What does the snippet named azd-project contain?")
```

```powershell
python agent_toolbox.py
```

Expected result (the wording of a model answer varies):

```text
Microsoft Learn shows the command is:

azd ai project set https://my-project.services.ai.azure.com/api/projects/my-project --no-prompt

Saved as snippet: `azd-project`
...
`azd-project` contains:

azd ai project set https://my-project.services.ai.azure.com/api/projects/my-project --no-prompt
```

One question used a tool from the Learn server and a tool from the Functions server. The agent code contains no server URL and no key.

## Step 4: Release version 2 without touching the agent

`promote.py` creates version 2 with only the snippets tool. Creating it does not change the default.

```powershell
python promote.py
python list_tools.py 2
python list_tools.py
```

Expected result (the tool descriptions are trimmed here):

```text
created version 2, default version is still 1
pro-code-agents/toolboxes/m2-docs-and-snippets/versions/2/mcp?api-version=v1
  snippets___get_snippet: ...
  snippets___hello_mcp: ...
  snippets___save_snippet: ...
snippets___hello_mcp -> Hello I am MCPTool!
pro-code-agents/toolboxes/m2-docs-and-snippets/mcp?api-version=v1
  mslearn___microsoft_code_sample_search: ...
  ...
```

The version endpoint shows the new list and the consumer endpoint still serves version 1. Promote version 2 and read the consumer endpoint again.

```powershell
python promote.py publish 2
python list_tools.py
```

```python
toolbox = project.toolboxes.update(name, default_version=sys.argv[2])
```

Expected result:

```text
default version is now 2
pro-code-agents/toolboxes/m2-docs-and-snippets/mcp?api-version=v1
  snippets___get_snippet: ...
  snippets___hello_mcp: ...
  snippets___save_snippet: ...
snippets___hello_mcp -> Hello I am MCPTool!
```

The same consumer URL now serves version 2. `agent_toolbox.py` would now see only the snippets tools, and nothing in it changed.

## Step 5: Consume the toolbox from a .NET host

`toolbox-cs` is an ASP.NET Core app that hosts an agent with the Responses protocol and registers the toolbox. It reads its settings from environment variables.

```csharp
var credential = new DefaultAzureCredential();
AIAgent agent = new AIProjectClient(new Uri(projectEndpoint), credential)
    .AsAIAgent(model: deploymentName, instructions: "Answer from Microsoft Learn and keep the snippets collection current. Keep answers under 60 words.", name: "m2-toolbox-host");

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.Services.AddFoundryToolboxes(credential, toolboxName);

var app = builder.Build();
app.MapFoundryResponses();
app.Run();
```

Both packages are preview releases. Start the host in one terminal.

```powershell
cd ../toolbox-cs
dotnet build
$env:AZURE_AI_PROJECT_ENDPOINT = "https://<resource>.services.ai.azure.com/api/projects/<project>"
$env:AZURE_AI_MODEL_DEPLOYMENT_NAME = "gpt-5.4"
$env:TOOLBOX_NAME = "m2-docs-and-snippets"
dotnet run --no-build
```

The log line `Toolbox 'm2-docs-and-snippets': discovered 3 tool(s).` appears when the host reads the toolbox, because version 2 is the default now. In a second terminal, send a request to the Responses endpoint.

```powershell
$body = '{"input":"Call the snippets hello tool and tell me what it said.","stream":false}'
$r = Invoke-RestMethod -Method Post -Uri http://localhost:47020/responses -ContentType application/json -Body $body
$r.status
$r.output | ForEach-Object { "$($_.type) $($_.name)" }
($r.output | Where-Object type -eq message).content.text
```

Expected result:

```text
completed
function_call snippets___hello_mcp
function_call_output 
message 
It said: **"Hello I am MCPTool!"**
```

## Verify

You are done when all of these hold:

- `list_tools.py` shows six tools from two servers on the consumer endpoint, and the snippets tool answers.
- The version endpoint for version 2 lists three tools while the consumer endpoint still lists six.
- After `promote.py publish 2`, the consumer endpoint lists three tools.
- The .NET host answers with a `function_call` to `snippets___hello_mcp` and a message that quotes the greeting.

## Cleanup

Stop the .NET host with Ctrl+C and delete the toolbox with all its versions.

```powershell
cd ../toolbox-py
python cleanup.py
```

Expected result:

```text
toolbox deleted
```

This demo reuses the project connection and the function app from the previous demo. Run the cleanup commands from that guide now to remove them.
