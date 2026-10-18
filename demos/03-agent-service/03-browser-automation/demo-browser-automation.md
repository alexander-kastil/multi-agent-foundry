# Drive a Managed Browser from an Agent

Some web applications offer no API, so a person has to open them, read them and click through them. In this demo you give a prompt agent a managed browser, let it open a page, follow a link and report what it saw, and watch every browser action it takes. You finish with a Playwright workspace, a project connection and a browsing agent that you control in Python and C#.

## Prerequisites

- A Microsoft Foundry project with a `gpt-5.4` deployment and `az login` completed.
- `azd` 1.36 or later with the `azure.ai.agents` and `azure.ai.connections` extensions (`azd ext install azure.ai.connections`).
- Contributor on the resource group (to create the workspace and the role assignment) and Foundry Project Manager on the project (to create the connection).
- Python 3.12 or later; the .NET 10 SDK for the C# project.
- Pages you browse are public. Do not point the agent at pages that hold sensitive data.

## Run it

```bash
cd demos/03-agent-service/03-browser-automation
bash provision.azcli
cd browser-automation-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python browser_automation.py
```

Edit `provision.azcli` first so the resource group, Foundry resource, project and region variables match your environment. After provisioning, paste the printed `BROWSER_CONNECTION_ID` and your `PROJECT_ENDPOINT` into `.env`.

## Step 1: Provision the Playwright workspace and the connection

The browser runs in a Playwright workspace, so the script creates one, gives the Foundry project identity the Playwright Workspace Contributor role on it and registers a project connection that uses that identity. Managed identity means no access token is stored anywhere.

```bash
az resource create --id $WORKSPACE_ID --api-version 2025-09-01 --location westeurope --properties '{"regionalAffinity":"Enabled","localAuth":"Enabled"}'

az role assignment create --assignee-object-id $PROJECT_PRINCIPAL --assignee-principal-type ServicePrincipal --role "Playwright Workspace Contributor" --scope $WORKSPACE_ID

azd ai connection create m3-browser-mcp --kind PlaywrightWorkspace --target https://westeurope.mcp.playwright.microsoft.com/playwrightworkspaces/$PLAYWRIGHT_GUID/mcp --auth-type project-managed-identity --audience https://mcp.playwright.microsoft.com --force -p $PROJECT_ENDPOINT --no-prompt
```

Expected output (trimmed):

```text
  "provisioningState": "Succeeded",
  "roleDefinitionName": "Playwright Workspace Contributor",
Connection "m3-browser-mcp" created in project "pro-code-agents".
Connection "m3-playwright-mcp" created in project "pro-code-agents".
PLAYWRIGHT_MCP_URL=https://westeurope.mcp.playwright.microsoft.com/playwrightworkspaces/7370266f-faec-4a0d-a714-c565c827cc71/mcp
PLAYWRIGHT_MCP_CONNECTION=m3-playwright-mcp
BROWSER_CONNECTION_ID=/subscriptions/cd091145-.../resourceGroups/rg-pro-code-agents/providers/Microsoft.CognitiveServices/accounts/pro-code-agents-foundry/projects/pro-code-agents/connections/m3-browser-mcp
```

The workspace is in West Europe while the Foundry project is in Sweden Central. They do not have to share a region, but the workspace region must be on the supported list. A role assignment can take a few minutes to take effect, so retry the next step if the first run reports a permission problem.

## Step 2: Give an agent the Browser Automation tool

The tool needs only the full resource ID of the project connection. The instructions teach the session discipline: one session, reused, closed at the end.

```python
tool = BrowserAutomationTool(
    browser_automation=BrowserAutomationToolParameters(
        connection=BrowserAutomationToolConnectionParameters(
            project_connection_id=os.environ["BROWSER_CONNECTION_ID"]
        )
    )
)

agent = project.agents.create_version(
    agent_name="m3-browser-agent",
    definition=PromptAgentDefinition(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        instructions=INSTRUCTIONS,
        tools=[tool],
    ),
)
conversation = openai.conversations.create()
```

Expected output:

```text
Created m3-browser-agent:7
```

## Step 3: Open a session and receive the Live View link

The first task only creates a session. Keeping it separate shows the session lifecycle on its own and lets you open Live View before anything happens in the browser.

```python
stream = openai.responses.create(
    stream=True,
    conversation=conversation.id,
    input=task,
    extra_body={"agent_reference": {"name": agent.name, "version": agent.version, "type": "agent_reference"}},
)
for event in stream:
    if event.type == "response.output_item.done" and event.item.type == "remote_function_call":
        arguments = json.loads(getattr(event.item, "arguments", None) or "{}")
        arguments.pop("browserSessionId", None)
        print(f"browser action: {event.item.name} {arguments}")
    elif event.type == "response.output_text.done":
        print("agent: live view URL received (kept private)" if "live.playwright.microsoft.com" in event.text else f"agent: {event.text}")
```

Expected output:

```text
== Task 1: Create a browser session and give me the live view link. Do not browse anywhere yet.
browser action: create_browser_session {}
agent: live view URL received (kept private)
```

The script removes `browserSessionId` from the printout and hides the Live View URL. Anyone who holds that URL can watch the session, so treat it like a secret.

## Step 4: Read a page

The second task reuses the session through the same conversation. The agent navigates, takes a snapshot of the page and answers only from that snapshot.

Expected output:

```text
== Task 2: Open https://example.com in that session. Report the page title and the description paragraph.
browser action: browser_navigate {'url': 'https://example.com'}
browser action: browser_snapshot {'depth': 4}
agent: Page title: Example Domain

Description paragraph: This domain is for use in documentation examples without needing permission. This is not a service; avoid relying on it for testing and monitoring purposes.
```

The `browser_snapshot` action returns the page structure with element references. That is why the agent can later click an element by reference instead of by pixel position.

## Step 5: Follow a link and close the session

The last task needs a multi-step workflow: click the link from the snapshot, wait, read the new page and close the session. The `target` in `browser_click` is the element reference from the snapshot.

Expected output:

```text
== Task 3: Follow the link in the description paragraph and report the heading of the page it leads to. Then close the browser session.
browser action: browser_click {'target': 'ref=e13'}
browser action: browser_snapshot {'depth': 4}
browser action: close_browser_session {}
agent: The heading is: Example Domains

The browser session is now closed.
Deleted m3-browser-agent:7
```

The exact element reference and the number of snapshots vary between runs, because the model decides each step from the page it sees. The sequence create, navigate, snapshot, click, snapshot, close is the part that stays stable.

## Step 6: Probe the Computer Use access gate

Computer Use uses a different model and a different loop, and its model is gated. This step checks whether your subscription can deploy it, because you cannot build the loop without a deployment.

```bash
az cognitiveservices account deployment create -g rg-pro-code-agents -n pro-code-agents-foundry --deployment-name m3-computer-use --model-name computer-use-preview --model-version 2025-03-11 --model-format OpenAI --sku-name GlobalStandard --sku-capacity 1
```

Expected output without access:

```text
ERROR: (SpecialFeatureOrQuotaIdRequired) The current subscription does not have access to this model 'Format:OpenAI,Name:computer-use-preview,Version:2025-03-11'.
```

Request access through the application form linked in the Computer Use documentation. After approval the same command creates the deployment, and the loop in the topic readme applies: send a screenshot, execute the returned action in a sandbox, send the next screenshot and acknowledge any pending safety checks.

## Verify

Task 2 and Task 3 answers contain text that exists only on the pages you opened, and the action trail shows `create_browser_session` before any navigation and `close_browser_session` last. Open the Playwright workspace in the Azure portal to confirm the sessions ended.

## Run the C# version

The C# project reaches the same workspace through an MCP tool, because `Azure.AI.Projects` 2.0.x exposes only the preview browser tool type. Copy the values printed in Step 1 into `.env`.

```bash
cd demos/03-agent-service/03-browser-automation/browser-automation-cs
cp .env.example .env
dotnet run
```

```csharp
McpTool browser = ResponseTool.CreateMcpTool(
    serverLabel: "playwrightWorkspace",
    serverUri: new Uri(Environment.GetEnvironmentVariable("PLAYWRIGHT_MCP_URL")!),
    toolCallApprovalPolicy: new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval));
browser.ProjectConnectionId = Environment.GetEnvironmentVariable("PLAYWRIGHT_MCP_CONNECTION")!;
```

Expected output:

```text
Created m3-browser-agent:9
== Task 2: Open https://example.com in that session. Report the page title and the description paragraph.
item: McpToolCallItem
item: McpToolCallItem
item: InternalResponsesAssistantMessage
agent: Opened https://example.com in the existing session.

- Title: Example Domain
- Description paragraph: This domain is for use in documentation examples without needing permission. This is not a service; avoid relying on it for testing and monitoring purposes.
== Task 3: Follow the link in the description paragraph and report the heading of the page it leads to. Then close the browser session.
item: McpToolCallItem
item: McpToolCallItem
item: McpToolCallItem
item: InternalResponsesAssistantMessage
agent: The link leads to a page with heading: Example Domains

The browser session has been closed.
```

Each `McpToolCallItem` is one browser action. The C# program prints item types rather than tool names, so the Python output is the better view of the action trail.

## Cleanup

The scripts delete the agent version and the Python script deletes its conversation. Remove the connections and the Playwright workspace with the deprovision script:

```bash
bash deprovision.azcli
```
