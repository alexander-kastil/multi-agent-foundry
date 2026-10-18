# Lessons

## Class layout

- Removing a module removes its theme from the class. "Remove 05-copilot-extensibility" means Microsoft 365 integration is out entirely: no Agents SDK, Copilot Studio, Teams or Copilot publishing, Agent 365 or Cowork topics anywhere in the layout, and no research spent on them. An uncommitted draft that still carries the theme does not bring it back.
- The class is the Microsoft agent stack only. A gap analysis never recommends LangGraph, LangChain or other non-Microsoft frameworks, low-code (Power Automate, Copilot Studio) or a prompt-engineering topic, however high market data ranks them. Semantic Kernel is retired in favour of Microsoft Agent Framework; never list it as a current option.
- "Compare and rate" a class means against market demand (job postings, developer surveys, Google Trends), not against a certification outline and not against the owner's own sister class repos.

## Demo authoring

- Class demo agents run their demos against the lab Foundry project and create what the demos need; the global "live mutations stay on the main thread" rule does not turn a class build into compile-only. Run-verify first is the create-class default, and the owner set no Azure limits.
- A correction to running agents never stops them. Update the contract file and pass the change through, or let them finish and apply it in a follow-up pass.
- The class ships demos in Python and C#. Never limit C# to topics where the archive happened to have a C# project; that rule was invented and left Module 1 Python-only.
- The class runs on its own Foundry resource and project: `pro-code-agents-foundry` / `pro-code-agents` in its own resource group `rg-pro-code-agents`, Sweden Central. Never on another class's resource or resource group, never a second Foundry resource per module.
