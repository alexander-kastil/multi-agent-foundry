# Function Tools, MCP & Approvals

Tools turn an agent from a text generator into something that acts: it reads a database, calls an API or books a hut. This topic covers the three ways to give an agent tools (your own functions, MCP servers and hosted tools that Foundry runs) and the approval gate that keeps a human in control of actions that cannot be undone.

## Function tools

A function tool is a Python callable or a .NET method that the model can call. In Python the `@tool` decorator reads the type hints and the docstring to build the tool schema; `Annotated` types with a description guide the model on each argument.

```python
from typing import Annotated
from agent_framework import tool
from pydantic import Field

@tool(approval_mode="never_require")
def get_trail_status(
    trail: Annotated[str, Field(description="Trail name, for example 'Wachau valley loop'.")],
) -> str:
    """Return the current status of a hiking trail."""
    return TRAILS.get(trail.lower(), "unknown trail")

agent = Agent(client=client, instructions="You are a trail guide.", tools=[get_trail_status])
```

The function runs in your process, and the model only sees its name, description and result. Write the docstring for the model, not for a colleague: it decides when the tool is used. The old `@ai_function` name is now `@tool`.

In C# the same idea is `AIFunctionFactory.Create`, which turns a method with `[Description]` attributes into an `AIFunction`.

```csharp
[Description("Return the current status of a hiking trail.")]
string GetTrailStatus([Description("Trail name.")] string trail) => ...;

AIAgent agent = projectClient.AsAIAgent(
    model: model,
    instructions: "You are a trail guide.",
    tools: [AIFunctionFactory.Create(GetTrailStatus, "GetTrailStatus")]);
```

## Approvals

Set `approval_mode="always_require"` on a tool that has side effects. The agent then stops before it executes the tool and returns the pending call in `result.user_input_requests`. Your code shows the call to a human and answers with `to_function_approval_response(True)` or `False`, sent back on the same session.

![The approval loop: book_hut stops the agent, a human approves or rejects, the answer goes back on the same session, and the loop repeats until no request is left](../pptx/images/05-02-approval-loop.svg)

```python
@tool(approval_mode="always_require")
def book_hut(hut: str, nights: int, persons: int) -> str:
    """Book a mountain hut. This charges the customer and cannot be undone."""

result = await agent.run("Book the Erzherzog Johann Hut for 2.", session=session)
while result.user_input_requests:
    replies = [
        Message(role="user", contents=[r.to_function_approval_response(True)])
        for r in result.user_input_requests
    ]
    result = await agent.run(replies, session=session)
```

An approval response only authorizes a call that matches a pending request recorded in the same `AgentSession`; the framework ignores unbound responses and logs a warning. In C# wrap the function in `ApprovalRequiredAIFunction`, look for `ToolApprovalRequestContent` in the response, and answer with `request.CreateResponse(true)` in a new user `ChatMessage`.

```csharp
AIFunction guarded = new ApprovalRequiredAIFunction(AIFunctionFactory.Create(BookHut, "BookHut"));
```

A rejected call is reported back to the model, which may ask again, so always loop until no request is left.

## MCP servers as tools

The Model Context Protocol lets an agent use tools that someone else hosts. Agent Framework connects in two different places, and the difference matters for security, networking and tracing.

| Option | Who calls the MCP server | Python | Use when |
|---|---|---|---|
| Local MCP tool over stdio | Your process starts the server | `MCPStdioTool` | The server is a local executable |
| Local MCP tool over HTTP | Your process connects | `MCPStreamableHTTPTool` | The server is remote but you want your process in control |
| Hosted MCP tool | The model service calls the server | `client.get_mcp_tool(name=..., url=...)` | The server is public and reachable from Foundry |

```python
from agent_framework import MCPStdioTool, MCPStreamableHTTPTool

trail_data = MCPStdioTool(
    name="trail-data",
    command=sys.executable,
    args=["trail_mcp_server.py"],
    approval_mode={
        "never_require_approval": ["get_forecast", "list_huts"],
        "always_require_approval": ["reserve_bed"],
    },
)

learn = MCPStreamableHTTPTool(name="microsoft-learn", url="https://learn.microsoft.com/api/mcp")

async with Agent(client=client, tools=[trail_data, learn]) as agent:
    ...
```

Use the tool as an async context manager so the connection opens and closes with the agent. On minimal installs add the `mcp` package yourself (`mcp>=1.24,<2`). `approval_mode` accepts `always_require`, `never_require`, or a dictionary that lists tool names per mode, and `allowed_tools` limits which remote tools the model can see.

A third-party MCP server receives the data you send it. Review what each server can read, prefer servers hosted by their owner, and keep credentials out of tool arguments by using `static_headers` or `header_provider` on `MCPStreamableHTTPTool`.

In C#, the `ModelContextProtocol` package creates the client and lists the tools, which are already `AIFunction` objects.

```csharp
await using McpClient learn = await McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
{
    Endpoint = new Uri("https://learn.microsoft.com/api/mcp"),
    Name = "microsoft-learn"
}));

IList<McpClientTool> mcpTools = await learn.ListToolsAsync();
AIAgent agent = projectClient.AsAIAgent(model: model, instructions: "...", tools: [.. mcpTools]);
```

## Hosted tools

Hosted tools run on the model service, not in your process. `FoundryChatClient` exposes them as static factories that return tool objects you pass to `tools=`; the older `Hosted*Tool` classes are gone.

| Tool | Factory on `FoundryChatClient` |
|---|---|
| Code interpreter | `get_code_interpreter_tool()` |
| File search | `get_file_search_tool(vector_store_ids=[...])` |
| Web search | `get_web_search_tool()` |
| Image generation | `get_image_generation_tool()` |
| Hosted MCP | `get_mcp_tool(name=..., url=..., approval_mode=...)` |

```python
agent = Agent(
    client=client,
    tools=[
        FoundryChatClient.get_code_interpreter_tool(),
        FoundryChatClient.get_mcp_tool(
            name="microsoft-learn",
            url="https://learn.microsoft.com/api/mcp",
            approval_mode="never_require",
        ),
    ],
)
```

`OpenAIChatClient` has the same factories with a different list; the Chat Completions client only offers web search. Trace context propagation to MCP servers works only for connections your process opens, not for hosted MCP tools, because the provider service issues those calls.

## Choosing where a tool runs

Use a function tool for logic you own and want to test. Use a local MCP tool when you reuse a server across agents and frameworks. Use a hosted tool when the model service already offers the capability, such as sandboxed code, and gate every tool with side effects behind an approval.

![Where tools run: function tools, MCPStdioTool and MCPStreamableHTTPTool in your process, the code interpreter and hosted MCP on the Foundry model service, and the Microsoft Learn MCP server reached from both](../pptx/images/05-01-where-tools-run.svg)

## Hands-On Demo

- [Equip an agent with tools, MCP servers and an approval gate](./demo-tools-approvals.md)

## Key Topics

- [Function tools](https://learn.microsoft.com/en-us/agent-framework/agents/tools/function-tools)
- [Tool approval](https://learn.microsoft.com/en-us/agent-framework/agents/tools/tool-approval)
- [Local MCP tools](https://learn.microsoft.com/en-us/agent-framework/agents/tools/local-mcp-tools)
- [Hosted MCP tools](https://learn.microsoft.com/en-us/agent-framework/agents/tools/hosted-mcp-tools)
- [Code interpreter](https://learn.microsoft.com/en-us/agent-framework/agents/tools/code-interpreter)
- [Microsoft Foundry model provider tools](https://learn.microsoft.com/en-us/agent-framework/integrations/by-component/model-providers/microsoft-foundry)
