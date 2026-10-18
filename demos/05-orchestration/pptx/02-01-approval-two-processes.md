---
slide: "02-01"
topic: 02-checkpoints-hitl
title: "One approval across two processes"
subtitle: "The pending request waits in the checkpoint folder"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/02-01-approval-two-processes.svg
order: 5
source: demos/05-orchestration/02-checkpoints-hitl/demo-checkpoints-hitl.md
gamma_prompt: >
  Slide titled "One approval across two processes", subtitle "The pending request waits in the
  checkpoint folder". Left half holds four short bullets about pausing in one process and answering
  in another. Right half shows the supplied diagram uncropped: process 1 submit.py with intake, the
  expense_summarizer agent and approval_gate on the left, an amber checkpoints folder listing seven
  checkpoints in the middle with superstep 3 highlighted as pending, and process 2 decide.py approve
  on the right ending in a green decision 1: APPROVE box. White background, clean minimal style.
---

# One approval across two processes

## Content

- `submit.py` runs intake, the summarizer agent and the approval gate, then exits at the **request**
- **FileCheckpointStorage** writes one checkpoint per superstep; superstep 3 holds the open request
- `decide.py` builds a new workflow object and resumes from the latest checkpoint
- The request comes back with the same id, and `responses=` delivers the answer

## Speaker Notes

This slide follows the demo from `submit.py` to `decide.py`. A human decision can arrive in a different process from the one that asked for it.

Process 1: `submit.py` runs `intake` and the `expense_summarizer` agent, and `approval_gate` calls `ctx.request_info`. The script prints the request id and exits, so nothing waits in memory.

Checkpoints on disk: `FileCheckpointStorage` writes a checkpoint at every superstep boundary, and `checkpoints.py` lists four of them. Only superstep 3 shows `pending_requests=1`, because that is where the gate is waiting.

Process 2: `decide.py` builds a fresh workflow object and loads the latest checkpoint with `storage.get_latest`. Running with `checkpoint_id` re-emits the pending request as a `request_info` event.

The answer: the re-emitted request carries the id that `submit.py` printed, and `run(responses=...)` delivers the decision without running the agent again. The resume and the answer add three more checkpoints to the folder.
