---
slide: "08-01"
topic: 08-ag-ui
title: "The AG-UI event stream"
subtitle: "One request, then typed events until the run ends"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/06-hosting/pptx/images/08-01-event-stream.svg
order: 20
source: demos/06-hosting/08-ag-ui/demo-ag-ui.md
gamma_prompt: >
  Slide titled "The AG-UI event stream", subtitle "One request, then typed events until the run ends". Two
  columns. Left column: the bullets. Right column, wider than the left: the provided diagram of one POST
  request followed by a Server-Sent Events stream (RUN_STARTED, a tool call block, an assistant message
  block, RUN_FINISHED) next to a chat window that shows how a client draws those events, shown as an inline
  content image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a
  background or side accent. Use exactly the one image given for this card; do not generate or add any other
  images, icons or logos.
---

# The AG-UI event stream

## Content

- The client posts **one JSON request** with `threadId`, `runId` and the messages, and keeps the connection open
- The server answers with **Server-Sent Events**, one JSON object per `data:` line
- `RUN_STARTED` and `RUN_FINISHED` frame the run, and tool calls and text messages stream in between
- The client decides how to draw each event type

## Speaker Notes

This slide shows what a front end receives when it asks the support desk agent about T-1002: the stream that `events.py` prints in lab Step 2.

One request: the body carries threadId t1, runId r1 and the user message. The thread id is what ties several runs into one conversation.

Server-Sent Events: every event is one JSON object on a `data:` line, and the stream ends with the run.

The order: the call to lookup_ticket arrives as start, arguments, end and result, then the answer as 19 TEXT_MESSAGE_CONTENT deltas. The Python server splits the arguments into nine fragments and also sends CUSTOM and MESSAGES_SNAPSHOT, so a client must not depend on those counts.

Drawing is the client's job: the protocol only fixes the vocabulary. The highlight steps through the events and lights the part of the chat each group feeds, the tool indicator first and the answer second.
