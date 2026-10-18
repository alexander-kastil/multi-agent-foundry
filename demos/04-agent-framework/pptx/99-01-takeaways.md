---
slide: "99-01"
topic: 04-agent-framework
title: "Takeaways"
subtitle: "What to take into your own agent projects"
layout: content
visual-weight: 1/3
visual-type: photo
media-source: nano-banana
media-file: demos/04-agent-framework/pptx/images/99-01-takeaways.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Two software engineers, a man in his early thirties and a woman in her late twenties, stand at a
  light-oak standing desk by a large window in a bright open-plan office after a successful run: he points at
  an open laptop while she holds a coffee cup and nods, both relaxed and smiling. Both people are in the centre
  of the frame, green plants behind them. The laptop screen stays blank of any readable text. Modern, light,
  bright and friendly: high-key natural daylight, white and light-oak contemporary interior, soft pastel accents
  in light blue, mint and lavender, green plants, 2020s design. Screens show only soft blurred shapes and
  colours, never readable UI. 2:3 portrait, people centred in the frame, contemporary editorial photography.
  Avoid: readable text, logos, watermarks, extra people, dark or moody lighting, vintage or retro decor, grime,
  clutter.
order: 20
source: demos/04-agent-framework/readme.md
gamma_prompt: >
  Closing slide titled "Takeaways", subtitle "What to take into your own agent projects". Content card: five
  short bullets on the left on plain white, the provided photograph of two engineers at a standing desk as a
  side accent on the right, no text over the photo. Use exactly the one image given for this card; do not
  generate or add any other images, icons or logos.
---

# Takeaways

## Content

- Choose the chat client by who owns the agent: **your code or Foundry**
- Keep one **AgentSession** per conversation and save it to resume after a restart
- Put memory in **context providers** and guardrails in **middleware**, outside the instructions
- Put **approvals** in front of actions that cannot be undone, and package procedures as **skills**
- Use a **harness agent** when a task needs a plan, todos and compaction

## Speaker Notes

Five points to take from this module into your own agent projects.

Chat client: swapping the client changes where the model runs while the agent code stays the same. The real decision is whether your code owns the agent definition or Foundry owns it.

Session: AgentSession carries a conversation from one run to the next. It is the unit you save when a conversation must survive a restart, a deployment or a different server.

Providers and middleware: history, preferences and long-term memory are all context providers, so one list on the agent controls them. Logging, guardrails and metering wrap the agent from the outside as middleware.

Approvals and skills: an approval gate keeps a human in control of actions that cannot be undone. Skills keep company procedures out of the system prompt until the task needs them.

Harness agent: plans, todos, standing approvals, compaction and file memory come bundled. You supply the chat client and the tools.
