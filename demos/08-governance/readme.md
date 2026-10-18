# Module 8: Security, Governance & Delivery

An agent that reaches production holds credentials, reads untrusted text and changes behavior with every prompt edit, so it needs the same controls as any other service. This module puts those controls around Microsoft Foundry agents, one layer at a time: guardrails at the platform, defenses in the prompt, an identity for every agent, isolation of tenants and secrets, quality gates in the pipeline, a lifecycle for versions, and a fairness and audit practice.

Every topic ships a runnable Python project built on `azure-ai-projects` 2.9.0 and the Responses API, and every demo runs against your Foundry project. The demos create agents and guardrails under `m8-` names and delete them when they end.

## Topics

| Topic | Description |
| --- | --- |
| [Guardrails: Prompt Shields & Intervention Points](01-guardrails/readme.md) | Controls at user input, tool call, tool response and output; Prompt Shields for user prompt and document attacks; task adherence |
| [Prompt Injection: Layered Defenses](02-prompt-injection/readme.md) | An attack corpus run against an undefended prompt, delimiters with a hardened prompt, and input and output checks |
| [Entra Agent ID: On-Behalf-Of Flows](03-agent-identity/readme.md) | A Microsoft Entra identity per agent, project managed identity versus agent identity, OAuth identity passthrough for MCP tools |
| [Zero Trust: Tenant Isolation & Secrets](04-zero-trust/readme.md) | Authentication flow selection, key rotation without downtime, secret-level access, and tenant partitions derived from a verified token |
| [CI/CD: GitHub Actions Quality Gates](05-cicd-gates/readme.md) | Dependency and contract gates, a canary gate on agent versions, a prompt regression gate, and the workflows that run them |
| [Agent Lifecycle: Versions, Quotas & Retirement](06-agent-lifecycle/readme.md) | Version manifests, staged rollout, the version selector on the stable endpoint, per-tenant token limits and notice-based retirement |
| [Responsible AI: Fairness Probes & Audit](07-responsible-ai/readme.md) | Counterfactual and distribution probes for unequal treatment, and an audit trail tied to agent versions |

## Demos

| Topic | Description |
| --- | --- |
| [Block Prompt Attacks at Every Intervention Point](01-guardrails/demo-guardrails.md) | Create a guardrail, attach it to an agent, block a jailbreak and an indirect attack in a tool response, and call Prompt Shields directly |
| [Measure Layered Defenses Against Prompt Injection](02-prompt-injection/demo-prompt-injection.md) | Run four injection attacks through three defense layers and add an attack of your own |
| [Give Agents Their Own Identity and Run an OAuth Consent Flow](03-agent-identity/demo-agent-identity.md) | Inspect two agent identities, grant one a least-privilege role on the other, and trigger an OAuth consent request |
| [Isolate Tenants and Rotate Secrets in a Multi-Tenant Agent](04-zero-trust/demo-zero-trust.md) | Verify a signed request, rotate a key, deny a cross-secret read, and watch an agent fail to read another tenant's data |
| [Block a Bad Agent Release with Quality Gates](05-cicd-gates/demo-cicd-gates.md) | Run the contract, canary and prompt regression gates and read the exit code each one returns |
| [Release, Pin, Meter and Retire an Agent Version](06-agent-lifecycle/demo-agent-lifecycle.md) | Gate a version with a manifest, pin the stable endpoint, meter tenants at a token gateway and retire a version with notice |
| [Probe an Agent for Unfair Treatment and Record an Audit Trail](07-responsible-ai/demo-responsible-ai.md) | Run fairness probes against a reviewer agent and read back the audit trail |

## Helpful Claude Slash Commands

| Command | Usage |
| --- | --- |
| `/security-review` | Analyze the changes on the current branch for injection, authentication and data exposure risks; run it on prompt, guard and workflow changes |
| `/permissions` | Manage allow, ask and deny rules for tool permissions, the least-privilege idea applied to Claude Code itself |
| `/code-review` | Review the current diff or a pull request for correctness before it reaches a gate or a workflow file |
| `/mcp` | Manage MCP server connections and their OAuth authentication, the client-side counterpart of the identity topic |
| `/usage` | Show session cost and usage, a small-scale view of the token metering that the lifecycle topic builds |

## Key Topics covered in this module

- [Guardrails and controls overview](https://learn.microsoft.com/azure/foundry/guardrails/guardrails-overview)
- [Prompt Shields in Microsoft Foundry](https://learn.microsoft.com/azure/foundry/openai/concepts/content-filter-prompt-shields)
- [Safety system messages](https://learn.microsoft.com/azure/foundry/openai/concepts/system-message)
- [Agent identity concepts in Microsoft Foundry](https://learn.microsoft.com/azure/foundry/agents/concepts/agent-identity)
- [Set up authentication for Model Context Protocol (MCP) tools](https://learn.microsoft.com/azure/foundry/agents/how-to/mcp-authentication)
- [Isolate hosted agent sessions per user](https://learn.microsoft.com/azure/foundry/agents/how-to/isolate-sessions-per-user)
- [Set up CI/CD for hosted agents with the Azure Developer CLI](https://learn.microsoft.com/azure/foundry/agents/how-to/set-up-ci-cd-cli)
- [Configure and share your agent: version selector](https://learn.microsoft.com/azure/foundry/agents/how-to/configure-agent)
- [Enforce token limits for models](https://learn.microsoft.com/azure/foundry/control-plane/how-to-enforce-limits-models)
- [Responsible AI for Microsoft Foundry](https://learn.microsoft.com/azure/foundry/responsible-use-of-ai-overview)
