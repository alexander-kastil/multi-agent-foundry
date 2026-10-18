---
slide: "07-02"
topic: 07-incident-response
title: "Snapshot replay and bisection"
subtitle: "Reproduce the failure and find the version without touching production"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/07-evaluation/pptx/images/07-02-replay-bisection.svg
order: 18
source: demos/07-evaluation/07-incident-response/demo-incident-response.md
gamma_prompt: >
  Slide titled "Snapshot replay and bisection", subtitle "Reproduce the failure and find the version
  without touching production". Left: four short bullets. Right, wider: the provided diagram of an
  amber snapshot card listing seven fields, a hash check and a replay box in the middle, and a bisection
  panel on the right with a green v1 row, a red v2 row and an amber result, shown whole and not cropped.
  Use exactly the one image given for this card; do not generate or add any other images, icons or
  logos.
---

# Snapshot replay and bisection

## Content

- A **replay snapshot** holds everything needed to run the trace again
- The **hash check** shows the captured prompt is not the current v1
- Replay feeds the **mocked tool response** and captured prompt to the same deployment
- **Bisection** replays each prompt version and names the first that fails

## Speaker Notes

A failed trace is only useful later if it was captured with everything needed to run it again, and this slide walks the lab's replay section.

Snapshot: every failed checkout writes a JSON file to replay-snapshots with the input, the model deployment, the prompt with its SHA-256 hash, the tool response with the order id masked as MOCK-A100, the trace timeline and the production output.

Hash check: comparing the snapshot's hash with the current v1 prompt reports a mismatch. That proves the snapshot came from a different prompt than v1.

Replay: the mocked tool response and the captured prompt go to the same gpt-5.4 deployment, and the output fails the format check again with valid False. The failure is reproduced without a single production call.

Bisection: the snapshot is replayed with each prompt version, read back from the Foundry agent versions with get_version. v1 is valid and v2 is not, so v2 is the first failing version.
