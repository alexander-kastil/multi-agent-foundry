---
slide: "02-02"
topic: 02-agent-session
title: "Saving and resuming a session"
subtitle: "to_dict, from_dict and the service conversation ID"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/02-02-save-resume-session.svg
order: 6
source: demos/04-agent-framework/02-agent-session/demo-agent-session.md
gamma_prompt: >
  Slide titled "Saving and resuming a session", subtitle "to_dict, from_dict and the
  service conversation ID". Left half holds four short bullets on persisting a
  Microsoft Agent Framework AgentSession: serialize it with to_dict, rebuild it in a
  new process with from_dict, rebuild it from the service conversation ID alone with
  get_session, and keep that ID in your own storage per user. Right half shows the
  supplied flow diagram from a first process through a session file to a second
  process and to another worker that only has the ID. Clean minimal style, white
  background, no extra imagery.
---

# Saving and resuming a session

## Content

- `session.to_dict()` turns the whole session into JSON you can write to a file
- **AgentSession.from_dict()** rebuilds it in a new process with the same agent setup
- **get_session** rebuilds a session from the service conversation ID alone
- Keep service IDs in **your own storage**, keyed by the authenticated user

## Speaker Notes

A conversation has to outlive the process that started it. The demo runs `session_file.py` twice as two separate processes, and the second one still knows about the Danube trip.

to_dict: persist the whole object, never only the message text, because provider state travels inside it. The first process writes `sessions/session.json` after its answer.

from_dict: the second process loads the file and passes the rebuilt session to `agent.run`. Restore it with the same agent and provider configuration that created it.

get_session: a worker that only has the ID calls `agent.get_session(service_session_id=...)` and continues the conversation. Treat `service_session_id` as opaque.

Your own storage: in an app with several users, map your user ID to a session and never accept a service ID from a client. A service ID is not an authorization boundary, so check ownership before resuming.
