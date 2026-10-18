# Block a Bad Agent Release with Quality Gates

An agent release can break callers, lose accuracy or weaken its defenses, and today none of that stops a merge. You run four gates that each end in an exit code, so a pipeline can refuse the release: a dependency and contract check, a canary check on Foundry agent versions, a prompt regression check, and the GitHub Actions workflows that run them.

What you practice here: turning an agent quality risk into a script whose exit code a pipeline can enforce.

Duration: about 50 minutes.

## Prerequisites

- Python 3.12 or later or the .NET 10 SDK, and `az login` completed against the subscription that holds your Foundry project.
- The Foundry User role on the project, and a `gpt-5.4` deployment.
- Concepts from the [topic readme](readme.md): exit codes as gates, the version selector and the regression thresholds.

## Run it

```bash
cd demos/08-governance/05-cicd-gates/cicd-gates-py
pip install -r requirements.txt
cp .env.example .env
python validate_compatibility.py
python canary_gate.py
python canary_gate.py regress
python prompt_regression.py
```

The C# twin has one entry point with three commands. Each ends with the same exit code.

```bash
cd demos/08-governance/05-cicd-gates/cicd-gates-cs
cp .env.example .env
dotnet run -- compat
dotnet run -- canary
dotnet run -- canary regress
dotnet run -- regression
```

Set `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env` first. Work in `cicd-gates-py/` or `cicd-gates-cs/`; both are finished projects. The scripts that call Foundry create agents under `m8-cicd-` names (C#: `m8h-cicd-`) and delete them before they exit.

## Step 1: Order the deployment from the dependency manifest

The manifest says which agent needs which. The script turns it into waves of agents that can deploy in parallel and writes the deploy workflow from the same graph.

```python
def deployment_waves(graph):
    waves = []
    done = set()
    while len(done) < len(graph):
        wave = sorted(n for n, deps in graph.items() if n not in done and set(deps) <= done)
        waves.append(wave)
        done.update(wave)
    return waves
```

Run `python validate_compatibility.py`. Expected output, first part:

```text
Deployment waves (agents in one wave deploy in parallel)
  wave 1: ingestion
  wave 2: security-scanner, style-checker, syntax-analyzer
  wave 3: performance-profiler
  wave 4: orchestrator
  wave 5: reporting
```

Open `out/deploy-agents.yml`. The `orchestrator` job lists `needs:` for four jobs, exactly the agents in the manifest line for it.

## Step 2: Block breaking contract changes

The same script compares `contracts/current.json` with `contracts/baseline.json`. In `current.json` the style checker now requires a `style_guide` parameter that used to be optional.

```python
for p in sorted(set(new["required_params"]) - set(old["required_params"])):
    reason = f"optional parameter '{p}' became required" if p in known else f"new required parameter '{p}'"
    found.append((agent, reason))
```

Expected output, rest of the run:

```text
Contract validation against baseline
  BREAKING style-checker: optional parameter 'style_guide' became required
           affected dependents: orchestrator, reporting

Generated out/deploy-agents.yml from the dependency manifest

Gate result: FAIL, breaking changes block deployment
```

Check the exit code with `echo $?` in Bash or `$LASTEXITCODE` in PowerShell. It is 1. The security scanner also changed, with a new optional parameter and a new response field, and the gate correctly ignores both.

![Seven agents in five deployment waves; style-checker carries a breaking change, the gate names orchestrator and reporting as affected dependents and exits with code 1](../pptx/images/05-01-dependency-contract-gate.svg)

### C#

`Compatibility.cs` reads the manifest with YamlDotNet and compares the contracts with `System.Text.Json.Nodes`. Run `dotnet run -- compat`; the output of Steps 1 and 2 is identical, and `echo $?` prints 1:

```csharp
foreach (string p in Strings(updated["required_params"]).Except(Strings(old["required_params"])).Order())
    found.Add((agent, known.Contains(p) ? $"optional parameter '{p}' became required" : $"new required parameter '{p}'"));
```

```text
Deployment waves (agents in one wave deploy in parallel)
  wave 1: ingestion
  wave 2: security-scanner, style-checker, syntax-analyzer
  wave 3: performance-profiler
  wave 4: orchestrator
  wave 5: reporting

Contract validation against baseline
  BREAKING style-checker: optional parameter 'style_guide' became required
           affected dependents: orchestrator, reporting

Generated out/deploy-agents.yml from the dependency manifest

Gate result: FAIL, breaking changes block deployment
```

## Step 3: Promote a canary that passes

The canary gate creates a stable version and a canary version of one agent. It pins the stable endpoint to the stable version first, so the test cannot change production traffic.

![The canary gate pins the stable endpoint to v1, creates canary v2, tests both versions by reference and moves the pin to v2 only when neither metric drops more than 5 points](../pptx/images/05-02-canary-gate.svg)

```python
stable = create_agent(NAME, STABLE, metadata={"role": "stable"})
pin_endpoint(NAME, stable.version)
canary = create_agent(NAME, variant, metadata={"role": "canary"})
```

Both versions answer six labeled code samples from `eval-set.jsonl`. Run `python canary_gate.py`. Expected output:

```text
Stable v1, canary v2, endpoint serves v1
baseline: {'accuracy': 0.8333333333333334, 'schema_valid_rate': 1.0}
canary:   {'accuracy': 1.0, 'schema_valid_rate': 1.0}
Quality gate PASS (threshold 5%): endpoint promoted to v2
```

The canary prompt is more precise, so its accuracy is higher and the gate promotes it by moving the pin to version 2. The exit code is 0. Accuracy numbers come from a model, so yours can differ; the pass condition is that neither metric drops more than 5 points.

### C#

The pin is a PATCH of the agent's endpoint with a fixed-ratio version rule. `Azure.AI.Projects.Agents` 2.0.0 has no typed method for it (`UpdateAgentAsync` is the full agent update and demands a `definition`), so `Foundry.cs` sends the merge-patch request with `HttpClient` and a token for `https://ai.azure.com/.default`. Reading the served version goes through the raw agent record.

```csharp
using StringContent content = new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/merge-patch+json");
HttpResponseMessage response = await http.PatchAsync($"{Endpoint}/agents/{name}?api-version=v1", content);
```

Run `dotnet run -- canary`. Expected output, exit code 0:

```text
Stable v1, canary v2, endpoint serves v1
baseline: {'accuracy': 0.8333333333333334, 'schema_valid_rate': 1.0}
canary:   {'accuracy': 1.0, 'schema_valid_rate': 1.0}
Quality gate PASS (threshold 5%): endpoint promoted to v2
```

## Step 4: Refuse a canary that regressed

The `regress` argument swaps in a prompt that tells the agent to ignore security and answer in prose. It breaks both metrics.

```python
variant = CANARY_REGRESSED if len(sys.argv) > 1 and sys.argv[1] == "regress" else CANARY_GOOD
```

Run `python canary_gate.py regress`. Expected output:

```text
Stable v1, canary v2, endpoint serves v1
baseline: {'accuracy': 0.8333333333333334, 'schema_valid_rate': 1.0}
canary:   {'accuracy': 0.0, 'schema_valid_rate': 0.0}
Quality gate FAIL (threshold 5%): endpoint stays on v1
```

The pin never moved, so consumers kept version 1, and the exit code is 1. In a pipeline this is the step that turns red.

### C#

Run `dotnet run -- canary regress`. Expected output, exit code 1:

```text
Stable v1, canary v2, endpoint serves v1
baseline: {'accuracy': 0.8333333333333334, 'schema_valid_rate': 1.0}
canary:   {'accuracy': 0.0, 'schema_valid_rate': 0.0}
Quality gate FAIL (threshold 5%): endpoint stays on v1
```

## Step 5: Gate a prompt change against regressions

The prompt regression gate treats two prompt files as two candidate versions and runs six tests against each. Two tests check answer quality, and four are injection attacks that must not succeed.

```python
quality_ok = candidate["quality"] >= baseline["quality"] - quality_tolerance
safety_ok = candidate["safety"] >= safety_threshold
print(f"DECISION: {'deploy v1.1.0' if quality_ok and safety_ok else 'keep v1.0.0'}")
```

Run `python prompt_regression.py`. Expected output:

```text
v1.0.0 -> Foundry agent version 1
v1.1.0 -> Foundry agent version 2

version   quality  safety
v1.0.0    0.50     1.00
v1.1.0    1.00     1.00

quality gate (no drop over 2%): PASS
safety gate (pass rate 100%): PASS
DECISION: deploy v1.1.0

agent versions deleted
```

Version 1.1.0 earns its release: its quality score rose from 0.50 to 1.00, and every injection test still passes. The first quality test looks for the phrase "requires clinician review", which only version 1.1.0 is told to include.

To see the gate fail, point each label at the other file in the `candidates` dictionary, so the weaker prompt becomes the candidate, and run the script again.

```python
candidates = {"v1.0.0": "prompts/v1.1.0.txt", "v1.1.0": "prompts/v1.0.0.txt"}
```

Expected output, trimmed:

```text
version   quality  safety
v1.0.0    1.00     1.00
v1.1.0    0.50     1.00

quality gate (no drop over 2%): FAIL
safety gate (pass rate 100%): PASS
DECISION: keep v1.0.0
```

The exit code is 1. Restore the original dictionary afterwards.

### C#

`PromptRegression.cs` runs the same six tests against two agent versions. Run `dotnet run -- regression`. Expected output, exit code 0:

```text
v1.0.0 -> Foundry agent version 1
v1.1.0 -> Foundry agent version 2

version   quality  safety
v1.0.0    0.50     1.00
v1.1.0    1.00     1.00

quality gate (no drop over 2%): PASS
safety gate (pass rate 100%): PASS
DECISION: deploy v1.1.0

agent versions deleted
```

To see the gate fail, swap the two paths in the `candidates` dictionary of `PromptRegression.cs` and run it again; the result matches the Python run above. Restore the dictionary afterwards.

## Step 6: Run the gates from GitHub Actions

The four workflows in `workflows/` wire the gates to triggers. Check which events and jobs each one defines.

```bash
python -c "import yaml, glob, os; [print(os.path.basename(f), list(d[True]), list(d['jobs'])) for f in sorted(glob.glob('workflows/*.yml')) for d in [yaml.safe_load(open(f))]]"
```

Expected output:

```text
automated-rollback.yml ['repository_dispatch'] ['rollback-agent']
canary-quality-gate.yml ['schedule'] ['assess-quality']
prompt-regression-gate.yml ['pull_request'] ['regression']
validate-compatibility.yml ['pull_request'] ['validate-contracts']
```

YAML 1.1 reads the key `on` as the boolean `True`, which is why the script indexes `d[True]`. Open `prompt-regression-gate.yml` and find the three parts that make a Foundry gate work in CI.

```yaml
permissions:
  id-token: write
  contents: read

      - uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Run prompt regression gate
        run: python prompt_regression.py
```

The permission lets the job request an OIDC token, the login step exchanges it for an Azure session, and the final step fails the job when the script exits 1. These workflows were not executed here because they need a GitHub repository with a federated credential; copy them to `.github/workflows/` there.

### C#

The four workflows in `workflows/` exist only in the Python project, and they call the Python scripts. The C# gates return the same exit codes, so a C# pipeline keeps the permissions and the `azure/login@v2` step and replaces the final step with `actions/setup-dotnet` followed by `run: dotnet run -- regression` in `cicd-gates-cs/`. That variant was not executed here.

## Verify

- Step 2 and Step 4 exit with code 1, and Step 3 and Step 5 exit with code 0.
- No `m8-cicd-` or `m8h-cicd-` agent remains in the project:

```bash
python -c "from foundry import project; print([a.name for a in project.agents.list() if a.name.startswith(('m8-cicd-', 'm8h-cicd-'))])"
```

Expected output:

```text
[]
```

## Cleanup

The scripts delete the agent versions they create. Remove the generated workflow.

```bash
rm -r out
```
