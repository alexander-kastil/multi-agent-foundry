# Ship a Hosted Agent with azd

Deploying an agent by hand means a ZIP or an image, an upload, a version, a status poll and a way to read its logs. The `azd ai agent` commands turn that into one loop you can repeat from any terminal or pipeline. In this demo you scaffold an azd project around a release-notes agent, run it locally, deploy it into the existing Foundry project as a hosted agent, invoke it, stream its logs and run the doctor check.

What you practice here: `azd ai agent init`, `run`, `invoke`, `monitor` and `doctor`, and `azd deploy` into an existing project.

Duration: about 50 minutes.

## Prerequisites

- Azure Developer CLI 1.25.2 or later and Azure CLI, both signed in (`az login`, `azd auth login`), and Python 3.10 or later, or the .NET 10 SDK for the C# version.
- Foundry Project Manager on the `pro-code-agents` project and a `gpt-5.4` deployment.
- The Foundry extensions: `azd ext install microsoft.foundry`.

## Run it

`azd` looks for `azure.yaml` in parent folders, and the course repository has one at its root. Copy the starter out of the repository and work in the copy; `azd init` also creates a git repository there, which is another reason not to run it in place.

```bash
cp -r demos/06-hosting/03-azd-ai-agent/azd-ai-agent-py ~/m6-azd
cd ~/m6-azd
```

```powershell
Copy-Item -Recurse demos\06-hosting\03-azd-ai-agent\azd-ai-agent-py $HOME\m6-azd
Set-Location $HOME\m6-azd
```

The starter holds only the agent source in `src/m6-azd-agent/`: `main.py` and a pinned `requirements.txt`. The finished project, with the generated `azure.yaml`, sits beside the starter in `azd-ai-agent-py-solution/`.

The C# version follows the same layout. Copy `azd-ai-agent-cs` instead; it holds `src/m6h3-azd-agent/` with `Program.cs` and `m6h3-azd-agent.csproj`, and the finished project with its `azure.yaml` is `azd-ai-agent-cs-solution/`. The C# agent is named `m6h3-azd-agent` in every command below.

```bash
cp -r demos/06-hosting/03-azd-ai-agent/azd-ai-agent-cs ~/m6h3-azd
cd ~/m6h3-azd
```

## Step 1: Initialize the azd project

`main.py` is an Agent Framework agent with one tool and the same `ResponsesHostServer` line as the hosted agent in the previous topic.

```python
@tool
def latest_release(product: str) -> str:
    """Return the latest release line for portal, gateway or mobile."""
    return RELEASES.get(product.lower(), f"No release data for {product}")

agent = Agent(
    client=client,
    name="m6-azd-agent",
    instructions="You are the Contoso release notes agent. Use latest_release and answer in one sentence.",
    tools=[latest_release],
)

if __name__ == "__main__":
    ResponsesHostServer(agent).run()
```

The C# agent has the same tool and the same Responses host as the hosted agent in the previous topic.

```csharp
AIFunction latestRelease = AIFunctionFactory.Create(
    (string product) => releases.GetValueOrDefault(product.ToLowerInvariant(), $"No release data for {product}"),
    "latest_release",
    "Return the latest release line for portal, gateway or mobile.");

AIAgent agent = new AIProjectClient(projectEndpoint, new DefaultAzureCredential())
    .AsAIAgent(
        model: deployment,
        instructions: "You are the Contoso release notes agent. Use latest_release and answer in one sentence.",
        name: "m6h3-azd-agent",
        tools: [latestRelease]);

var builder = AgentHost.CreateBuilder(args);
builder.Services.AddFoundryResponses(agent);
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());
```

`init` needs the resource id of the project. Build it from your subscription id; in Git Bash set `MSYS_NO_PATHCONV=1` first, otherwise the leading slash is rewritten into a Windows path.

```bash
export MSYS_NO_PATHCONV=1
SUB=$(az account show --query id -o tsv | tr -d '\r')
PROJECT_ID="/subscriptions/$SUB/resourceGroups/rg-pro-code-agents/providers/Microsoft.CognitiveServices/accounts/pro-code-agents-foundry/projects/pro-code-agents"
```

```powershell
$sub = az account show --query id -o tsv
$projectId = "/subscriptions/$sub/resourceGroups/rg-pro-code-agents/providers/Microsoft.CognitiveServices/accounts/pro-code-agents-foundry/projects/pro-code-agents"
```

Run init with the project id (use `$projectId` in PowerShell). Passing `--model-deployment` reuses the existing `gpt-5.4` deployment instead of provisioning a model.

```bash
azd ai agent init --src src/m6-azd-agent --agent-name m6-azd-agent \
  --project-id "$PROJECT_ID" --model-deployment gpt-5.4 \
  --deploy-mode code --runtime python_3_13 --entry-point main.py \
  --protocol responses --no-prompt
```

Expected output (the banner is omitted):

```text
Let's get your project initialized.
Initializing an app to run on Azure (azd init)
  (✓) Done: Initialized git repository
Added agent 'm6-azd-agent' to azure.yaml.
You can customize environment variables and other settings in the agent service entry in azure.yaml.
```

For C#, the same command points at the C# source, asks for the .NET runtime and names the compiled entry point:

```bash
azd ai agent init --src src/m6h3-azd-agent --agent-name m6h3-azd-agent   --project-id "$PROJECT_ID" --model-deployment gpt-5.4   --deploy-mode code --runtime dotnet_10 --entry-point m6h3-azd-agent.dll   --protocol responses --no-prompt
```

Expected output (the banner is omitted):

```text
Let's get your project initialized.
Initializing an app to run on Azure (azd init)
  (✓) Done: Initialized git repository
Added agent 'm6h3-azd-agent' to azure.yaml.
OK: .NET runtime validation passed: m6h3-azd-agent.csproj targets net10.0, selected runtime is .NET 10.
```

In the C# `azure.yaml`, `language` is `csharp`, `codeConfiguration.runtime` is `dotnet_10` and `entryPoint` is `m6h3-azd-agent.dll`; everything else is the same.

Open the new `azure.yaml`. The agent is a service with `host: azure.ai.agent`, `kind: hosted` and a `codeConfiguration` block; it `uses` a second service, `host: azure.ai.project`, that holds the project endpoint. The model deployment name is mapped into the agent through `env: AZURE_AI_MODEL_DEPLOYMENT_NAME`, and the project endpoint reaches the agent as `FOUNDRY_PROJECT_ENDPOINT`.

## Step 2: Run the agent locally

`run` detects the Python project, creates `.venv`, installs `requirements.txt` and starts the same host on port 8088. `--no-client` skips Agent Inspector, which would otherwise open in a browser.

```bash
azd ai agent run --no-client
```

Expected output:

```text
Detected python project. Start command: python main.py
Setting up Python environment...
Using CPython 3.14.2
Creating virtual environment at: .venv
Activate with: source .venv/Scripts/activate
Installing dependencies (requirements.txt)...
  ✓ Dependencies installed (requirements.txt)
Starting agent on http://localhost:8088 (Ctrl+C to stop)
```

Leave it running and invoke it from a second terminal in the same folder with `--local`.

```bash
azd ai agent invoke --local "What is the latest portal release?"
```

Expected output:

```text
Target:       localhost:8088 (local)
Message:      "What is the latest portal release?"
Session:      9ecf9722-5251-459f-9711-b4a7c3de5b1d
Conversation: d376d4dd-d557-46b9-831c-5237713513c3
[local] The latest portal release is 2.4.0, shipped 2026-10-01, and it adds SSO and a new audit log.
Client elapsed: 3.950s
```

For C#, `run` detects the .NET project and starts it with `dotnet run`; the first call can take much longer than in Python while the credential chain is resolved and the project is built:

```bash
azd ai agent run --no-client
azd ai agent invoke --local "What is the latest portal release?"
```

Expected output (C#, trimmed):

```text
Detected dotnet project. Start command: dotnet run
Starting agent on http://localhost:8088 (Ctrl+C to stop)
```

```text
Target:       localhost:8088 (local)
Message:      "What is the latest portal release?"
[local] The latest portal release is 2.4.0, shipped 2026-10-01, and it adds SSO plus a new audit log.
Client elapsed: 31.456s
```

Stop the host with Ctrl+C before you deploy.

## Step 3: Deploy into the existing project

The project already exists, so `azd deploy` is enough; `azd up` would also run the provisioning step. The deployment packages the folder as a ZIP, creates an agent version, waits until it is active and registers the agent environment variables.

```bash
azd deploy
```

Expected output (progress lines trimmed):

```text
  m6-azd-agent: Done [1m13s]
- Agent playground (portal): https://ai.azure.com/nextgen/r/.../build/agents/m6-azd-agent/build?version=1
- Agent endpoint (responses): https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents/agents/m6-azd-agent/endpoint/protocols/openai/responses?api-version=v1
Next:
  azd ai agent show m6-azd-agent
  verify it's running
  azd ai agent invoke m6-azd-agent '<payload>'
  test the deployment
SUCCESS: Your application was deployed to Azure in 1 minute 13 seconds.
```

For C#, `azd deploy` publishes the project, zips it and creates the version the same way:

```text
  m6h3-azd-agent: Deploying (Deploying hosted agent (code deploy)) [4s]
  m6h3-azd-agent: Deploying (Creating new agent from code package) [5s]
  m6h3-azd-agent: Deploying (Waiting for agent to become active) [8s]
  m6h3-azd-agent: Done [1m30s]
- Agent endpoint (responses): https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents/agents/m6h3-azd-agent/endpoint/protocols/openai/responses?api-version=v1
SUCCESS: Your application was deployed to Azure in 1 minute 32 seconds.
```

Check the result with `show` (use `m6h3-azd-agent` for C#).

```bash
azd ai agent show m6-azd-agent
```

```text
Name                             m6-azd-agent
Version                          1
Status                           active
Endpoint (responses)             https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents/agents/m6-azd-agent/endpoint/protocols/openai/responses?api-version=v1
```

`show` also prints the agent's instance identity and its blueprint, the dedicated Entra identity described in the previous topic. Running `azd deploy` again creates version 2 of the same agent.

## Step 4: Invoke the deployed agent

Without `--local`, `invoke` sends the message to the agent endpoint with your Entra token. The first call after idle time pays for provisioning the sandbox, which the latency breakdown shows.

```bash
azd ai agent invoke m6-azd-agent "What changed in the mobile app last?"
```

Expected output (ids differ):

```text
Conversation: conv_0d8963048f07646500egby6N4Y3y8egkgpirSqoqyax4wVen2i
Session:      06ced8d2d27246a42b9e66c46356f01dce49f8e7530d6863a76085c42c40c52 (assigned by server)
Trace ID:     4a8bd628438bf7277293862ac8a1cf85
Response:     caresp_0d8963048f07646500Boclnh9trCl4MV5qsidpO6niMIsUncTf
[m6-azd-agent] The latest mobile app release was 3.1.0 on 2026-10-12, and it added offline mode.
Client elapsed: 12.615s
Platform latency (cold): response headers 8478 ms
  preprocess 106 ms | infra 1394 ms | readiness 6113 ms | container 865 ms
```

For C#, run `azd ai agent invoke m6h3-azd-agent "What changed in the mobile app last?"`:

```text
[m6h3-azd-agent] The latest mobile app release was 3.1.0 on 2026-10-12, and it added offline mode.
Client elapsed: 8.758s
Platform latency (cold): response headers 5077 ms
  preprocess 157 ms | infra 1816 ms | readiness 2167 ms | container 937 ms
```

The answer comes from the `latest_release` tool running in the platform sandbox, not on your machine. The trace id leads to the trace in Application Insights.

## Step 5: Stream the container logs

`monitor` attaches to the session of the last invocation and prints the container's console output. Use `--follow` to keep it open and `--type system` for container events.

```bash
azd ai agent monitor m6-azd-agent --tail 50
```

Expected output (lines trimmed, ids and times differ):

```text
Streaming session logs for m6-azd-agent (session: 402f067ad3fa16addb3625e0238b6a780633e1870ab7544fb6a6a7a98ae1b7b)...
09:23:43  status   Connecting to the container...
09:23:35  stderr   2026-10-18 07:23:35,685 INFO azure.ai.agentserver: AgentServerHost started
09:23:35  stderr   2026-10-18 07:23:35,686 INFO azure.ai.agentserver: Platform environment: is_hosted=True, agent_name=m6-azd-agent, agent_version=1, port=8088, session_id=402f067ad3fa16addb3625e0238b6a780633e1870ab7544fb6a6a7a98ae1b7b, ...
09:23:35  stderr   [2026-10-18 07:23:35 +0000] [35] [INFO] Running on http://0.0.0.0:8088 (CTRL + C to quit)
09:23:43  status   Successfully connected to container
```

The C# agent logs through .NET, so the same facts appear as `AgentServer platform environment: IsHosted=True AgentName=m6h3-azd-agent AgentVersion=1 Port=8088 SessionId=...` on `stdout`.

`is_hosted=True` with the agent name and version confirms the platform injected its settings, the same effect you saw in the previous topic.

## Step 6: Diagnose with doctor

`doctor` checks the project before you have to guess. It reads `azure.yaml` and the azd environment, tests authentication, then asks the Foundry project whether your role is sufficient and the hosted agent is active.

```bash
azd ai agent doctor
```

Expected output:

```text
azd ai agent doctor
Local
   (✓) azd extension reachable
   (✓) azure.yaml present and parseable
   (✓) azd environment selected
   (✓) agent service in azure.yaml
   (✓) FOUNDRY_PROJECT_ENDPOINT set
   (✓) agent definition valid (per service)
   (✓) manual env vars set
   (-) Configured toolboxes have endpoint env vars set -- skipped (no configured toolbox resources were found.)
Authentication
   (✓) authentication
Remote
   (✓) Foundry project endpoint reachable
   (✓) Developer has required role on Foundry project
   (✓) Hosted agents are active
   (-) Configured connections exist on Foundry project -- skipped (no enabled connection services found.)
   (-) Project storage permissions -- skipped (no customer-owned Storage connections are bound to the project capability host.)
11 passed, 0 failed, 3 skipped
```

The C# project ends with the same line, `11 passed, 0 failed, 3 skipped`. Skipped checks are not failures: they apply to toolboxes, connections and customer-owned storage, none of which this agent uses.

## Verify

You have verified the demo when `azd ai agent show` reports `Status active`, step 4 answers with the mobile release from the tool, and `doctor` ends with `0 failed`.

## Cleanup

Delete the agent with azd. `azd down` is for environments azd provisioned, and it would try to remove shared infrastructure, so it is not used with an existing project.

```bash
azd ai agent delete m6-azd-agent --force --no-prompt
```

```text
Agent "m6-azd-agent" deleted.
```

For C#, delete `m6h3-azd-agent` and remove `~/m6h3-azd`:

```bash
azd ai agent delete m6h3-azd-agent --force --no-prompt
```

```text
Agent "m6h3-azd-agent" deleted.
```

Remove the working copy with `rm -rf ~/m6-azd` (or `Remove-Item -Recurse -Force $HOME\m6-azd` in PowerShell).
