---
slide: "00-01"
topic: 05-orchestration
title: "Workflows & Multi-Agent Orchestration"
subtitle: "Business processes on the Agent Framework workflow engine"
layout: title-only
visual-weight: bg
visual-type: photo
media-source: nano-banana
media-file: demos/05-orchestration/pptx/images/00-01-workflows-orchestration.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen, no diptych, no
  collage. Two software engineers, a woman and a man in their early thirties, stand at a large whiteboard
  wall in a bright modern engineering office: she draws a flow of rectangles joined by arrows that splits
  into three parallel boxes and joins again, he holds an open laptop and points at one of the boxes. Both
  people are in the centre of the frame, a tall plant and a window with daylight behind them. The
  whiteboard shows only boxes and arrows with no writing, and the laptop screen stays blank of any text.
  Modern, light, bright and friendly: high-key natural daylight, white and light-oak contemporary
  interior, soft pastel accents in light blue, mint and lavender, green plants, 2020s design. Screens show
  only soft blurred shapes and colours, never readable UI. 2:3 portrait, people centered in the frame,
  contemporary editorial photography. Avoid: readable text, logos, watermarks, extra people, dark or moody
  lighting, vintage or retro decor, grime, clutter.
order: 1
source: demos/05-orchestration/readme.md
gamma_prompt: >
  Title slide "Workflows & Multi-Agent Orchestration" with subtitle "Business processes on the Agent
  Framework workflow engine". Side-accent layout: title, subtitle and three bullets on the left half on
  plain white, the provided photograph of two engineers sketching a branching flow on a whiteboard filling
  the right half, no text over the photo. Use exactly the one image given for this card; do not generate
  or add any other images, icons or logos.
---

# Workflows & Multi-Agent Orchestration

## Content

- **Workflows** as typed graphs, with checkpoints and human requests
- **Orchestration builders** for sequential, concurrent, handoff, group chat and Magentic patterns
- **Applied patterns**: planners, resilient hubs and declarative YAML workflows

## Speaker Notes

A single agent with tools handles a question, but a business process needs steps in a fixed order, parallel checks, approvals that can take a day and failures that do not take the whole system down. This module builds those processes with the Microsoft Agent Framework 1.x workflow engine on a Microsoft Foundry project.

Workflows: executors and edges form a typed graph, and the superstep model makes execution predictable. Checkpoints and human requests let a run pause and resume in another process.

Orchestration builders: sequential, concurrent and handoff come as ready-made builders, and group chat and Magentic put a manager in the middle. You pass in agents and get back an ordinary workflow.

Applied patterns: the last three topics decompose a task with a planner, keep a hub running when spokes fail and write workflows as YAML or plain Python. They also cover the migration of Foundry portal workflows before the designer retires on December 1, 2026.
