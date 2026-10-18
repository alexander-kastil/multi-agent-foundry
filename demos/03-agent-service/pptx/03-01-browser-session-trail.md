---
slide: "03-01"
topic: 03-browser-automation
title: "The browser session lifecycle"
subtitle: "Six browser actions across three tasks in one session"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/03-01-browser-session-trail.svg
order: 8
source: demos/03-agent-service/03-browser-automation/demo-browser-automation.md
gamma_prompt: >
  Slide titled "The browser session lifecycle", subtitle "Six browser actions across three
  tasks in one session". Left half: four short bullets on the browserSessionId from
  create_browser_session, answering from browser_snapshot, clicking by element reference,
  and closing the session before the 15 minute cleanup. Right half: the supplied diagram of
  three tasks, the six browser actions in order and the agent's answer after each task,
  shown whole without cropping. Clean minimal style, white background.
---

# The browser session lifecycle

## Content

- `create_browser_session` returns a **browserSessionId** that every later action carries
- After `browser_snapshot` the agent answers **only from the snapshot**
- `browser_click` targets an **element reference** such as `ref=e13`, not a pixel position
- `close_browser_session` ends the session; an **inactive session** is cleaned up after 15 minutes

## Speaker Notes

The demo sends three tasks through one conversation, and the agent keeps one browser session for all of them. The highlight follows the action trail that the Python script prints.

Session id: Task 1 only creates the session and returns the Live View link, which the script keeps private. Anyone who holds that URL can watch the session, so treat it like a secret.

Snapshot: browser_snapshot returns the page structure with element references. The agent reports the page title Example Domain from that snapshot alone.

Element reference: Task 3 clicks the link by its reference from the snapshot, reads the new page and reports the heading Example Domains. The reference and the number of snapshots vary between runs, while the order create, navigate, snapshot, click, snapshot, close stays stable.

Closing: an inactive session is cleaned up after 15 minutes, so the instructions ask for explicit cleanup. The trail shows create_browser_session first and close_browser_session last.
