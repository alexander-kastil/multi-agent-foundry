---
slide: "07-01"
topic: 07-declarative-workflows
title: "Pause and resume in a functional workflow"
subtitle: "Finished @step results come from the cache"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/07-01-functional-pause-resume.svg
order: 17
source: demos/05-orchestration/07-declarative-workflows/demo-declarative-workflows.md
gamma_prompt: >
  Slide titled "Pause and resume in a functional workflow", subtitle "Finished @step results come from
  the cache". Two columns. Left column: the four bullets. Right column, wider than the left: the provided
  diagram of two runs in lanes, the first run calling write_draft and pausing at ctx.request_info with
  state IDLE_WITH_PENDING_REQUESTS, a reviewer answer in the middle, and the resume bypassing write_draft
  from the cache before revise_draft produces the final announcement, shown as an inline content image at
  its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a background or
  side accent. Use exactly the one image given for this card; do not generate or add any other images,
  icons or logos.
---

# Pause and resume in a functional workflow

## Content

- `@workflow` and `@step` write the pipeline as one async Python function
- `ctx.request_info` pauses the run, which ends as `IDLE_WITH_PENDING_REQUESTS`
- The resume passes the reviewer's answer as `responses={"review": ...}`
- `@step` results are **cached**: `write_draft` is bypassed and only the reviser calls the model

## Speaker Notes

The functional API writes a workflow as plain async Python with `await`, `if`, loops and `asyncio.gather`. It is experimental and Python only, and this slide follows the announcement review from the demo.

One function: `write_draft` and `revise_draft` are `@step` functions that call the writer and the reviser agent. Call `.build()` on the `@workflow` definition first, because the definition itself has no `run` method.

The pause: `ctx.request_info` sends the draft out with `request_id="review"`, and the first run ends with state `IDLE_WITH_PENDING_REQUESTS`. The explicit id lets the resume call name the request.

The resume: the second `pipeline.run` passes the reviewer's feedback under that id. The function then runs again from the top.

The cache: `@step` caches a result by function name and bound arguments, so `write_draft` emits `executor_bypassed` and reuses the draft. Only `revise_draft` calls the model, and that is the line the Verify step checks.
