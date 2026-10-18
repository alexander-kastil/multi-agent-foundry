---
slide: "08-01"
topic: 08-voice-agents
title: "Events of a realtime voice session"
subtitle: "The same loop for every turn, over one WebSocket"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/08-01-realtime-session-events.svg
order: 21
source: demos/03-agent-service/08-voice-agents/readme.md
gamma_prompt: >
  Slide titled "Events of a realtime voice session", subtitle "The same loop for every turn, over one
  WebSocket". Two columns. Left column: the bullets. Right column, wider than the left: the provided sequence
  diagram between your client and the realtime session, with the connect and session-ready events, a dashed
  frame around the events repeated for each user turn and two result boxes below, shown as an inline content
  image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a background
  or side accent. Use exactly the one image given for this card; do not generate or add any other images,
  icons or logos.
---

# Events of a realtime voice session

## Content

- Wait for the **session-ready** event before the first turn
- Send the turn with `conversation.item.create`, then ask for a reply with `response.create`
- Collect **`response.audio.delta`** chunks and the transcript until `response.done`
- Audio is **PCM16, mono, 24 kHz**; the demo saves each reply as a WAV file

## Speaker Notes

Both paths to a voice agent use the same long-lived WebSocket session, so this one event loop covers the voice agent and Voice Live alike.

Session ready: the client connects and waits for the first event. A voice agent sends session.created, which also carries the conversation id, and the Voice Live session answers with session.updated.

One turn, two events: the client sends the user turn as a message item, as text in the demos or as input audio, and then response.create to ask for a reply.

The reply streams: audio arrives as base64 chunks in response.audio.delta events, the text in response.audio_transcript.done, and response.done closes the turn. The dots on the diagram walk two turns.

The audio format: PCM16, mono, 24 kHz in both directions. The demos join the chunks into WAV files, so no microphone or speaker is needed.
