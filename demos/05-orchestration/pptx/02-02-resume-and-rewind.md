---
slide: "02-02"
topic: 02-checkpoints-hitl
title: "Resume and rewind"
subtitle: "Pick the checkpoint that matches what should run again"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/02-02-resume-and-rewind.svg
order: 6
source: demos/05-orchestration/02-checkpoints-hitl/demo-checkpoints-hitl.md
gamma_prompt: >
  Slide titled "Resume and rewind", subtitle "Pick the checkpoint that matches what should run
  again". Left half holds four short bullets comparing a resume with a rewind. Right half shows the
  supplied diagram uncropped: a timeline of four amber checkpoint circles, superstep 0 to 3, with
  two branches below, a rewind from superstep 1 where the summarizer agent runs again and raises a
  new request, and a resume from superstep 3 that keeps the same request id and ends in decision 1:
  APPROVE. White background, clean minimal style.
---

# Resume and rewind

## Content

- Any checkpoint is a valid **start point** for `decide.py`
- **Resume** at superstep 3: same request id, the stored summary, no new agent call
- **Rewind** to superstep 1: the summarizer runs again and raises a new request
- Agent calls after the chosen superstep repeat and are billed

## Speaker Notes

After the clean resume, the demo rewinds to an earlier superstep. This slide compares both runs of `decide.py` against the same four checkpoints.

Start point: any checkpoint can start a run, and `decide.py` takes an optional superstep number to pick one. Superstep 0 is the entry checkpoint with the workflow input.

Resume: `decide.py approve` continues from superstep 3, where the gate is waiting. The request id is the one `submit.py` printed, and the decision uses the summary stored in the checkpoint.

Rewind: `decide.py reject 1` starts before the summarizer ran, so the agent runs again. The gate raises a new request with a new id, and the summary wording changes.

Cost: resuming from an earlier superstep replays everything after it. Use a rewind to recover from a bad step, and expect the repeated agent calls to be billed.
