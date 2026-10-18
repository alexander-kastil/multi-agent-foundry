---
slide: "07-01"
topic: 07-harness-agent
title: "Plan and execute modes"
subtitle: "One session carries the todos, the mode and a standing approval"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/07-01-plan-execute-modes.svg
order: 17
source: demos/04-agent-framework/07-harness-agent/demo-harness-agent.md
gamma_prompt: >
  Slide titled "Plan and execute modes", subtitle "One session carries the todos, the mode and a standing
  approval". Two columns. Left column: four short bullets. Right column, wider than the left: the provided
  diagram of a plan run, a mode switch by the application and an execute run above one shared session, shown
  as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never
  used as a background or side accent. Use exactly the one image given for this card; do not generate or add
  any other images, icons or logos.
---

# Plan and execute modes

## Content

- **Plan mode**: the agent checks the stages, writes its own todos and stops before booking
- **set_agent_mode** switches the session to `execute`, which lets the agent act
- **Standing approval**: one `create_always_approve_tool_response` covers all three `reserve_hut` calls
- **AgentSession** holds history, todos, mode and approvals, so reuse one per conversation

## Speaker Notes

This slide follows the trek planner through its first two demo steps: plan, then execute. Both runs use the same session.

Plan mode: the agent read all four stages with get_stage_status, wrote three todos and asked for confirmation. Nothing was reserved yet, and the wording of the todos differs on every run.

Switching modes: the application calls set_agent_mode(session, "execute"), which queues a one-time notification for the next run. The plan-to-execute confirmation is instruction-level behavior, not a tool approval.

Standing approval: reserve_hut is marked always_require, and the script answers the first request with create_always_approve_tool_response. The standing rule approved the second and third call without asking, so one approval produced three reservations.

One session: todos, mode, approvals and history all live in the AgentSession, so reuse it for the whole conversation. Start a new session when approvals should not carry forward.
