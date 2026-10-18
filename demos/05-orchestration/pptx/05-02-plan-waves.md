---
slide: "05-02"
topic: 05-task-decomposition
title: "Running the plan in waves"
subtitle: "A planner draws the graph and a short loop runs it"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/05-02-plan-waves.svg
order: 12
source: demos/05-orchestration/05-task-decomposition/demo-task-decomposition.md
gamma_prompt: >
  Slide titled "Running the plan in waves", subtitle "A planner draws the graph and a short
  loop runs it". A narrow text column with four short bullets beside a large diagram on white.
  Left: query and AGENT_REGISTRY feed a blue meta_agent_planner box that returns a grey JSON
  plan where T1, T2 and T3 have empty depends_on and T4 depends on T1, T2 and T3. Top right:
  execute_plan as three amber steps in a row (find ready tasks, run the wave in parallel with
  asyncio.gather, store results in completed) with a loop-back arrow labelled "while pending".
  Bottom: a red guard box where the cycle_check.py plan raises ValueError, and a grey trace
  panel showing Wave 1 with T1, T2, T3, Wave 2 with T4 and a green final output from T4. Clean
  minimal style, no drop shadows.
---

# Running the plan in waves

## Content

- **meta_agent_planner** turns the query and the registry into a JSON plan with `depends_on`
- A **wave** is every task whose dependencies are complete, run with `asyncio.gather`
- Results go into `completed` and become **context** for the next wave
- No ready task is a **guard**: a cycle raises `ValueError` instead of looping forever

## Speaker Notes

In the second strategy a planner agent draws the split at run time, and a short loop in your code runs it. The complex demo query produces four subtasks.

Planner: the planner receives the query and the registry of specialists and returns raw JSON. Each subtask names its agent and lists `depends_on`, so the plan is a directed acyclic graph your code can run.

Waves: `execute_plan` collects the tasks whose dependencies are all complete and runs them together with `asyncio.gather`. For the complex query, wave 1 runs T1, T2 and T3, and wave 2 runs T4.

Context: each task receives the results of the tasks it depends on, so the thesis in T4 starts from the three earlier results. The loop repeats while tasks are pending.

Guard: a planner is a model, so its output is untrusted input. `cycle_check.py` feeds a plan in which T1 and T2 wait for each other, and the empty ready check raises a `ValueError` instead of spinning forever.
