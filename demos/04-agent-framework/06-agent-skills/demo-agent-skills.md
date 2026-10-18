# Teach an Agent a Company Procedure with Skills

A hut desk that pastes its whole booking policy into every prompt pays for it on every call, and one edit risks breaking the rest. In this demo you give an agent a hut-booking skill made of a procedure, a policy, a template and a price script, measure what each loading stage costs, and add a code-defined skill next to it. You finish with an agent that follows a written procedure, runs a script under approval and loads only what each question needs.

What you practice here: packaging a procedure as a SKILL.md folder and loading it progressively.

Duration: about 40 minutes.

## Prerequisites

- Python 3.12 or later, the Azure CLI signed in with `az login`, and a Foundry project with a `gpt-5.4` deployment.
- For the C# step: .NET 10 SDK and `python` on the path, because the shipped script runner starts it.

## Run it

```powershell
cd agent-skills-py
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
Copy-Item .env.example .env
```

On macOS or Linux, activate with `source .venv/bin/activate` and copy with `cp .env.example .env`. Fill in `PROJECT_ENDPOINT` and `MODEL_DEPLOYMENT_NAME` in `.env`. The finished projects are `agent-skills-py/` and `agent-skills-cs/`.

## Step 1: Inspect the skill and run its script by hand

The skill lives in `skills/hut-booking/`. `SKILL.md` carries the frontmatter the agent sees first and the procedure it loads on demand.

```text
skills/hut-booking/
├── SKILL.md
├── references/CANCELLATION_POLICY.md
├── assets/confirmation-template.md
└── scripts/price_quote.py
```

```markdown
---
name: hut-booking
description: Quote, book and cancel mountain hut stays for Contoso Outdoor. Use when a customer asks for a hut price, a booking confirmation or the cancellation rules.
license: Proprietary
compatibility: Requires python3
metadata:
  owner: contoso-outdoor-bookings
  version: "1.0"
---

# Hut booking procedure

Follow these steps for every hut request.

1. Read `references/CANCELLATION_POLICY.md` when the customer asks about changes or refunds.
2. Run the `price_quote.py` script to calculate the price. Pass the arguments as positional values in this order: nights, persons, season (`summer` or `winter`). Never calculate prices yourself.
3. Fill in `assets/confirmation-template.md` with the quote and show it to the customer.
4. State the cancellation window from the policy in the last sentence.
```

Run the script yourself to know what the agent will get back.

```powershell
python skills/hut-booking/scripts/price_quote.py 3 4 winter
```

Expected output:

```text
total=624 euro rate=52 euro per person and night
```

The `description` is the only part the agent reads before deciding to use the skill, so it says both what the skill does and when to use it. The order of the numbered steps is the contract the script arguments depend on.

## Step 2: Give the skill to an agent and watch the four stages

`SkillsProvider.from_paths` discovers the skills, and a `script_runner` lets the agent execute `price_quote.py`. All three skill tools require approval by default, so the script approves each request and prints it.

```python
skills = SkillsProvider.from_paths(
    skill_paths=Path(__file__).parent / "skills",
    script_runner=run_script,
)

agent = Agent(
    client=build_client(),
    name="HutDesk",
    instructions="You are the hut desk of Contoso Outdoor. Use skills when they match the request.",
    context_providers=[skills],
)

result, steps = await run_with_approvals(
    agent,
    "Quote 3 nights for 4 hikers in winter and tell me the cancellation rules.",
    session,
)
```

```powershell
python run_file_skills.py
```

Expected output:

```text
decided: load_skill({"skill_name":"hut-booking"})
decided: read_skill_resource({"skill_name":"hut-booking","resource_name":"references/CANCELLATION_POLICY.md"})
decided: read_skill_resource({"skill_name":"hut-booking","resource_name":"assets/confirmation-template.md"})
decided: run_skill_script({"skill_name":"hut-booking","script_name":"scripts/price_quote.py","args":["3","4","winter"]})
# Contoso Outdoor hut quote

Nights: 3  
Hikers: 4  
Season: winter  
Total: 624 euro  
Cancellation: Free cancellation up to 14 days before arrival; 50% refund between 13 and 3 days before arrival; no refund within 2 days of arrival or after no-show.

The cancellation window is free up to 14 days before arrival.
```

The total 624 euro is 3 nights x 4 hikers x 52 euro, so the number came from the script, not from the model. Run `python run_file_skills.py reject` and the script rejects every request; the agent never loads the procedure and asks which hut you mean instead.

## Step 3: Measure progressive disclosure

A chat middleware records the input tokens of every model call. Three runs show the cost of having skills and the cost of using one.

```python
async def token_meter(context: ChatContext, call_next) -> None:
    await call_next()
    usage = context.result.usage_details or {}
    calls.append(usage.get("input_token_count", 0))
```

```powershell
python run_progressive.py
```

Expected output:

```text
without skills: 31 input tokens
advertise only: 682 input tokens
model call 1: 696 input tokens
model call 2: 990 input tokens
model call 3: 1244 input tokens
approved: load_skill({"skill_name":"hut-booking"})
approved: read_skill_resource({"skill_name":"hut-booking","resource_name":"references/CANCELLATION_P
approved: read_skill_resource({"skill_name":"hut-booking","resource_name":"assets/confirmation-templ
approved: run_skill_script({"skill_name":"hut-booking","script_name":"scripts/price_quote.py","args"
```

Read the numbers as stages. The 682 tokens of "advertise only" include the skill descriptions and the three skill tool definitions; call 2 adds the loaded `SKILL.md`; call 3 adds the policy, the template and the script result. A greeting never pays for the policy.

## Step 4: Mix a code-defined skill and auto-approve the harmless tools

`altitude_skill.py` defines a second skill in code. Its `convert` script runs in your process, so it needs no runner. `AggregatingSkillsSource` combines it with the file skills, and `ToolApprovalMiddleware` auto-approves `load_skill` and `read_skill_resource` while `run_skill_script` still asks.

```python
altitude_skill = InlineSkill(
    frontmatter=SkillFrontmatter(name="altitude", description="Convert mountain altitudes between meters and feet. ..."),
    instructions="Use the convert script. Pass the value and the factor (3.28084 for meters to feet, 0.3048 for feet to meters).",
)

@altitude_skill.script(name="convert", description="Convert a value: result = value x factor")
def convert(value: float, factor: float) -> str:
    return json.dumps({"value": value, "factor": factor, "result": round(value * factor, 1)})

skills = SkillsProvider(
    DeduplicatingSkillsSource(
        AggregatingSkillsSource([
            FileSkillsSource(Path(__file__).parent / "skills", script_runner=run_script),
            InMemorySkillsSource([altitude_skill]),
        ])
    )
)

agent = Agent(
    client=build_client(),
    name="HutDesk",
    instructions="You are the hut desk of Contoso Outdoor. Use skills when they match the request.",
    context_providers=[skills],
    middleware=[ToolApprovalMiddleware(auto_approval_rules=[SkillsProvider.read_only_tools_auto_approval_rule])],
)
```

```powershell
python run_mixed_skills.py
```

Expected output:

```text
asked for approval: run_skill_script({"skill_name":"altitude","script_name":"convert","args":{"value":3019,"factor":3.28084}})
3019 meters is 9904.9 feet.

If a group member is hurt there, follow these steps:
1. Secure the group and stop moving on exposed terrain.
2. Call 140 and give the exact position.
3. Wait with the group and keep warm.

Mountain rescue in Austria: 140.
```

One question used two skills. The agent loaded `trail-safety` for the emergency steps and `altitude` for the conversion without asking, and only the script call reached you for approval. Code-defined arguments arrive as a JSON object, file-based ones as a list of strings.

## Step 5: Load the same folder from C#

`AgentSkillsProviderBuilder` assembles the file skills, the code-defined skill and a script runner. The csproj copies `skills/` next to the binary, and `ScriptRunner.cs` starts `python` for file-based scripts.

```csharp
var altitude = new AgentInlineSkill(
        name: "altitude",
        description: "Convert mountain altitudes between meters and feet. ...",
        instructions: "Use the convert script. ...")
    .AddScript("convert", (double value, double factor) =>
        JsonSerializer.Serialize(new { value, factor, result = Math.Round(value * factor, 1) }));

var skillsProvider = new AgentSkillsProviderBuilder()
    .UseFileSkill(Path.Combine(AppContext.BaseDirectory, "skills"))
    .UseSkill(altitude)
    .UseFileScriptRunner(ScriptRunner.RunAsync)
    .Build();
```

```powershell
cd ../agent-skills-cs
Copy-Item .env.example .env
dotnet run
```

Expected output:

```text
approved: load_skill(skillName=hut-booking)
approved: load_skill(skillName=altitude)
approved: read_skill_resource(skillName=hut-booking, resourceName=assets/confirmation-template.md)
approved: read_skill_resource(skillName=hut-booking, resourceName=references/CANCELLATION_POLICY.md)
approved: run_skill_script(skillName=hut-booking, scriptName=scripts/price_quote.py, arguments=["3","4","winter"])
approved: run_skill_script(skillName=altitude, scriptName=convert, arguments={"value":3019,"factor":3.28084})
# Contoso Outdoor hut quote

Nights: 3  
Hikers: 4  
Season: winter  
Total: 624 euro  
...
3019 meters is 9904.9 feet.
```

The tool argument names are `skillName` and `scriptName` in C# and `skill_name` and `script_name` in Python; the folder and the procedure are identical.

## Verify

Both languages return a total of 624 euro and 9904.9 feet, and in both the first tool call is `load_skill`. Check that the skill names match their folders.

```powershell
Get-ChildItem skills -Directory | Select-Object -ExpandProperty Name
```

Expected output:

```text
hut-booking
trail-safety
```

Both names equal the `name` field in their `SKILL.md`, which the frontmatter rules require.

## Cleanup

Nothing was created in Azure.

```powershell
deactivate
Remove-Item -Recurse -Force .venv
```
