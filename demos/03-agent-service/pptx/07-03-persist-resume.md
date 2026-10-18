---
slide: "07-03"
topic: 07-agentic-loops
title: "Persist and resume a session"
subtitle: "The application keeps the ids, Foundry keeps the history"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/07-03-persist-resume.svg
order: 20
source: demos/03-agent-service/07-agentic-loops/demo-agentic-loops.md
gamma_prompt: >
  Slide titled "Persist and resume a session", subtitle "The application keeps the ids, Foundry keeps the
  history". Two columns. Left column: the bullets. Right column, wider than the left: the provided diagram of
  a first process that writes a small sessions.json file, a second process that reads it and gets the resumed
  answer, and a wide band below for the conversation that Foundry Agent Service stores, shown as an inline
  content image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a
  background or side accent. Use exactly the one image given for this card; do not generate or add any other
  images, icons or logos.
---

# Persist and resume a session

## Content

- `loops.py` writes the **conversation id**, the last response id and metadata to `sessions.json`, then exits
- `resume.py` is a **separate process** that loads the entry by session name
- A question that names no ticker still gets **GOOG** and its 24.8 P/E
- Store the **agent version** with the id to know which version produced the history

## Speaker Notes

This slide shows the fourth loop design: work that stops in one process and continues in another.

Only ids are stored: the end of loops.py opens a conversation, asks about GOOG and saves the conversation id, the last response id and a metadata object under analyst-session-1. Open sessions.json to see how little the application keeps.

A new process: resume.py never ran the original turn. It loads the entry and continues the same conversation by passing its id.

The proof: the follow-up asks which ticker was just discussed and what its P/E ratio was, without naming either. The answer GOOG with 24.8 can only come from the history on the server.

The agent version: a conversation can continue on a newer version, but the stored version tells you what produced the history. Store it next to the conversation id when behavior must stay stable across a resume.
