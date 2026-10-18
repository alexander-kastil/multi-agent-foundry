---
slide: "00-02"
topic: 03-agent-service
title: "Overview"
subtitle: "The path through the module"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/00-02-overview.svg
order: 2
source: demos/03-agent-service/readme.md
gamma_prompt: >
  Slide titled "Overview", subtitle "The path through the module". Two columns. Left column: the bullets.
  Right column, wider than the left: the provided diagram of eight numbered topic cards in three rows read
  left to right, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the
  column, never cropped, never used as a background or side accent. Use exactly the one image given for this
  card; do not generate or add any other images, icons or logos.
---

# Overview

## Content

- Start with **prompt agents**: versions, `name:version` references and conversations
- Add built-in tools and a managed browser for code, files, the web and user interfaces
- Ground answers in **Foundry IQ** knowledge bases and tuned hybrid search
- Keep state with **memory stores** and loops that plan, reflect and resume
- Finish with **voice agents** that listen and answer in speech

## Speaker Notes

This slide walks the eight topics once, in the order the module builds them.

Prompt agents first: agent versions, name:version references, conversations and responses are the primitives every later topic uses.

Tools: Code Interpreter, File Search and Web Search run without a tool server, and Browser Automation drives a managed browser for systems that have no API.

Knowledge: Foundry IQ plans retrieval across several sources and returns cited content, and the advanced RAG topic opens the box with hybrid search, reranking and routing, measured by mean reciprocal rank.

State: memory stores bring facts back in later sessions, and the agentic loops topic plans, reflects, forks and resumes from stored ids.

Voice: the last topic puts the Voice Live API in front of a text agent and builds a voice-based prompt agent.
