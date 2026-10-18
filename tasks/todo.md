# Step 1: Rebuild demos/ on the agreed 8-module layout

The old content is archived under `archive/demos/` (read-only source). The new tree is scaffolded under `demos/`: every module folder has `readme.md` with its H1, every topic folder has `readme.md` with its H1. The master TOC `demos/readme.md` and the root `readme.md` are owned by the coordinator. Step 1 works in `demos/` only: no `labs/` work.

## Shared contract for every module agent

### Ownership

- You own exactly one folder: `demos/<NN-module>/`. Write nothing outside it.
- Read-only sources: `archive/demos/`, `D:\git-classes\ai-500\demos\`, `src/`, `labs/`. Copy from them, never move, edit or delete them. Never copy `.venv`, `.env`, `__pycache__`, `bin`, `obj`, `node_modules`.
- Never run `git checkout --`, `git restore`, `git stash`, `git reset`, `git mv`, `git commit` or `git push`. Do not spawn subagents.

### Method

Follow the `create-class` skill by reading its files (do not call it as a Skill): `C:\Users\AlexanderKastil\.claude\skills\create-class\SKILL.md`, then `references/create-teaching.md`, `references/create-guide.md` (Template A, code walkthrough), `references/port-legacy-module.md` (porting from the archive) and `references/run-foundry-demo.md`. The repo-local brand voice is `.claude/skills/brand-voice-code/` (read `SKILL.md` and `references/rules.md`, apply it to every Markdown file you write).

The create-guide confirmation gate is settled by this contract: format `walkthrough` (code), audience developer, demos only, runtime and sources as listed per module below.

### Per topic deliverables

1. `readme.md`: keep the H1 exactly as scaffolded. Concept-first learning prose under descriptive subheadings (never an instructor bullet outline), real syntax for every mechanism it explains, a `## Hands-On Demo` section linking `demo-<slug>.md`, and a `## Key Topics` list of verified Microsoft Learn links. No lab link in step 1.
2. `demo-<slug>.md` in the topic folder: H1 is a verb-action title; Template A shape (problem first, Prerequisites, Run it, Step sections with the real snippet, Expected result, Verify, Cleanup). A demo has 4 to 6 substantive steps and runs 15 minutes or more.
3. Runnable project beside the guide: `<slug>-py/` for Python, `<slug>-cs/` for C#. Every demo ships in Python and C#; where one language has no honest equivalent for a step, the guide says so. Each project ships `requirements.txt` (pinned) or `.csproj`, `.env.example` (`PROJECT_ENDPOINT`, `MODEL_DEPLOYMENT_NAME`, plus what the demo needs) and its entry script. When a demo builds code live, the starter must not contain the deliverable; otherwise ship the finished project only. The `-solution` sibling is created from the run-verified code.

### Module readme

Keep the H1 `# Module N: <Title>`. Then: a use-case intro, a `| Topic | Description |` table linking each topic folder, a `| Topic | Description |` demo table linking each `demo-<slug>.md`, a topic-specific `## Helpful Claude Slash Commands` table (commands verified to exist), and `## Key Topics covered in this module` with verified Microsoft Learn links.

### Writing and code rules

- "Microsoft Foundry", never "Azure AI Foundry". No em dashes. Max 3 sentences per paragraph. Every code fence declares a language (prompts are `text`). Mermaid labels are `"quoted<br/>labels"`.
- No Microsoft 365 integration anywhere: no M365 Agents SDK, Copilot Studio, Teams or Copilot publishing, Activity protocol, Agent 365, Cowork, Work IQ, Power Automate.
- Code: no inline comments, no explanatory remarks, no error handling unless the demo teaches it. `DefaultAzureCredential` for auth. Current APIs only: `azure-ai-projects` 2.x `create_version` / conversations / responses, never classic threads, runs or `create_agent`; Agent Framework 1.x names (`Agent`, `AgentSession`, `@tool`, `FoundryChatClient`, `AsAIAgent`).
- Validate every product fact, status (GA or preview) and API name against Microsoft Learn before writing it. Verify package versions on PyPI or NuGet at write time and pin them. Baseline seen on 2026-10-18: `azure-ai-projects` 2.9.0, `agent-framework` 1.22.0, `agent-framework-foundry` 1.15.0, `agent-framework-orchestrations` 1.3.2, `agent-framework-ag-ui` 1.6.0, `azure-ai-evaluation` 1.18.9, `Microsoft.Agents.AI` 1.25.0, `Azure.AI.Projects` 2.0.x, `Microsoft.Extensions.AI` 10.11.0, `ModelContextProtocol` 2.2.0.

### Run-verify against Azure

Every demo is run for real before its guide is final (create-guide "Run-verify first").

- Azure context: `az` is signed in as alexander.kastil@integrations.at on subscription "Visual Studio Enterprise Subscription" (`cd091145-5ea2-4703-ba5d-41063b1d4308`). The Foundry project for this class is `pro-code-agents` on its own Foundry resource `pro-code-agents-foundry` in resource group `rg-pro-code-agents`, Sweden Central: `PROJECT_ENDPOINT=https://pro-code-agents-foundry.services.ai.azure.com/api/projects/pro-code-agents`. Deployments: `gpt-5.4` (default chat), `gpt-5-mini`, `text-embedding-3-small`. Never use `ai-103-demos-resource` or anything in `rg-ai-103`. Put the values in each project's own `.env` (gitignored); never commit, print or alter credentials.
- Allowed: create and use project-scoped items in `pro-code-agents` (agents and versions, conversations, toolboxes, memory stores, knowledge bases, routines, evaluations, hosted agent deployments), deploy an extra model into the same Foundry resource when a demo needs it, and create Azure resources a demo needs (AI Search, Application Insights, Storage, Function App, Container Registry, Durable Task Scheduler) in resource group `rg-pro-code-agents` in Sweden Central. Role assignments only at the scope of the project or of resources you created.
- Prefix every name you create with `m<N>-` (your module number) so parallel modules never collide. Run the guide's Cleanup step for project-scoped items; keep shared Azure resources and list each one in your report (name, type, resource group).
- Never delete or change anything you did not create.
- You may kill processes in this session: servers you started, stuck runs, and build or language servers that lock a folder you need.
- Capture real output and paste it, trimmed, into the guide's Expected output blocks. When a demo builds code live, create the `-solution` sibling from the run-verified code. When a demo cannot run (gated model, region, quota, missing access), say exactly why in the report and describe the expected result in prose; never invent output.
- Every project carries its own `.gitignore`: Python `.venv/`, `__pycache__/`, `*.pyc`, `.env`; C# `bin/`, `obj/`, `.vs/`, `*.user`, `.env`; Node `node_modules/`, `dist/`, `.env`; Azure Functions also `local.settings.json`.
- Before reporting, run `python C:\Users\AlexanderKastil\.claude\skills\create-class\scripts\fence-lang.py demos\<NN-module>` and `bash C:/Users/AlexanderKastil/.claude/skills/create-class/scripts/link-check.sh --root D:/git-classes/pro-code-agents --scope demos/<NN-module>`; both must be clean.

## Modules

- [ ] Module 1 `demos/01-foundry-essentials` (ms-foundry-expert)
- [ ] Module 2 `demos/02-mcp` (dotnet-expert)
- [ ] Module 3 `demos/03-agent-service` (ms-foundry-expert)
- [ ] Module 4 `demos/04-agent-framework` (maf-expert)
- [ ] Module 5 `demos/05-orchestration` (maf-expert)
- [ ] Module 6 `demos/06-hosting` (ms-foundry-expert)
- [ ] Module 7 `demos/07-evaluation` (ms-foundry-expert)
- [ ] Module 8 `demos/08-governance` (ms-foundry-expert)
