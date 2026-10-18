---
slide: "03-03"
topic: 03-agent-identity
title: "Least privilege between agents"
subtitle: "The Foundry Agent Consumer role at the scope of one agent"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/03-03-agent-scope.svg
order: 9
source: demos/08-governance/03-agent-identity/demo-agent-identity.md
gamma_prompt: >
  Slide titled "Least privilege between agents", subtitle "The Foundry Agent Consumer
  role at the scope of one agent". Left half: four short bullets on the assignee, the
  role's single data action, the agent scope and the project scope. Right half: a
  diagram with three boxes in a row (orders agent identity, Foundry Agent Consumer
  role, HR agent endpoint) above two wide scope rows, a green agent scope row and a
  grey project scope row. Clean minimal style, white background, show the whole
  diagram without cropping.
---

# Least privilege between agents

## Content

- The **orders agent identity** is the assignee, by its principal id
- **Foundry Agent Consumer** holds one data action: interact
- At the **agent scope** the role reaches only the `m8-idn-hr` endpoint
- At the project scope the same role reaches **every agent endpoint**

## Speaker Notes

An agent that calls another agent over A2A is a consumer like any other, and its identity is the assignee. Step 3 of the demo grants exactly that.

Orders agent identity: the assignment uses the identity's object (principal) ID and the role ID instead of the role name, because the Foundry roles were renamed recently.

Foundry Agent Consumer: its single data action is `Microsoft.CognitiveServices/accounts/AIServices/endpoints/interact/action`. The orders agent can call the HR agent, but it cannot create or change agents.

Agent scope: the scope ends in `/agents/m8-idn-hr`, so the role covers that one endpoint. Listing the assignments at that scope shows exactly one entry.

Every agent endpoint: assigned at the project scope, the same role would open every agent endpoint in the project.
