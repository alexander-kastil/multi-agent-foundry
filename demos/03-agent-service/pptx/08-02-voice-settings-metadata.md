---
slide: "08-02"
topic: 08-voice-agents
title: "Voice settings in the agent metadata"
subtitle: "The voice travels with the agent version"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/08-02-voice-settings-metadata.svg
order: 22
source: demos/03-agent-service/08-voice-agents/demo-voice-agents.md
gamma_prompt: >
  Slide titled "Voice settings in the agent metadata", subtitle "The voice travels with the agent version".
  Two columns. Left column: the bullets. Right column, wider than the left: the provided diagram of a voice
  configuration stored in the metadata of an agent version, a client that connects through the Voice Live API
  by agent name and version, and two green results showing the voice from the metadata and the facts from the
  instructions, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column,
  never cropped, never used as a background or side accent. Use exactly the one image given for this card; do
  not generate or add any other images, icons or logos.
---

# Voice settings in the agent metadata

## Content

- Voice, turn detection, noise suppression and echo cancellation go under **`microsoft.voice-live.configuration`**
- A metadata value holds at most **512 characters**, so longer JSON continues in numbered keys
- The client names the agent, its version and the project, and sets **no voice** itself
- `session.updated` reports the stored voice; the facts in the answer come from the **instructions**

## Speaker Notes

This slide shows the Voice Live path of the demo: an ordinary text agent that carries its own voice.

The settings live on the agent: voice, turn detection, noise suppression and echo cancellation are stored in the agent's metadata. Any client that connects to the agent gets the same voice.

The 512-character limit: chunk_config splits the JSON across numbered keys when it is longer than one metadata value can hold. A new agent version changes the behavior and the voice together.

The client stays thin: it connects with the agent name, version and project over Microsoft Entra ID, because agent mode does not accept keys. The session code sets modalities and the PCM16 format, with no instructions and no voice.

Proof in the output: session.updated names en-US-Ava:DragonHDLatestNeural, which came from the metadata. The answer about the Summit 4S being rated to minus 25 degrees Celsius came from the instructions, so the Voice Live service called the agent on every turn.
