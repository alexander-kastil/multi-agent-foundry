---
slide: "05-02"
topic: 05-foundry-toolkit
title: "Agent Inspector and a local agent"
subtitle: "Three terminals: the agent, a question and the inspector"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/01-foundry-essentials/pptx/images/05-02-agent-inspector.svg
order: 15
source: demos/01-foundry-essentials/05-foundry-toolkit/demo-foundry-mcp.md
gamma_prompt: >
  Slide titled "Agent Inspector and a local agent", subtitle "Three terminals: the agent,
  a question and the inspector". Left half holds five short bullets: terminal 1 starts the
  local agent on localhost:8088 with the Responses protocol; terminal 2 posts a question
  to /responses with ask.py or dotnet run -- ask; the output items function_call,
  function_call_output and message show the tool ran first; terminal 3 runs azd ai
  inspector launch, which serves the UI at 127.0.0.1:8087; the inspector shows the
  request, the tool call, the streamed events and the final output. Right half shows the
  supplied diagram of the three terminals around the local agent and its output items.
  Clean and light, white background.
---

# Agent Inspector and a local agent

## Content

- **Terminal 1** starts the local agent on `localhost:8088` with the Responses protocol
- **Terminal 2** posts a question to `/responses` with `ask.py` or `dotnet run -- ask`
- The output items show the **tool ran first**: `function_call`, `function_call_output`, `message`
- **Terminal 3** runs `azd ai inspector launch`, which serves the UI at `http://127.0.0.1:8087`
- The inspector shows the **request**, the tool call, the streamed events and the final output

## Speaker Notes

Steps 5 and 6 of the demo need three terminals, and this slide shows what each one runs and what talks to what.

Start the agent first: `local-agent/main.py` and `local-agent-cs` host an Agent Framework agent with the `get_weather` tool on port 8088. Opening the inspector does not start your agent, so it has to be running already.

A question over HTTP: `ask.py` and the C# `ask` command post "What is the weather in Graz?" to `/responses` on the local agent. The Python server binds all network interfaces, while the C# host binds `localhost` only.

Proof of the tool call: the answer comes back as `function_call`, `function_call_output` and `message`. The three items show that the agent called its tool before it answered.

The inspector: `azd ai inspector launch` serves the UI at `http://127.0.0.1:8087` and targets port 8088 by default. Use `--port` and `--inspector-port` when either port is taken.

What it shows: send the step 5 message in the inspector playground, and the response, the `get_weather` call with its arguments and the streamed events appear in the UI. In VS Code with Foundry Toolkit the same view opens from Developer Tools, Build, Agent Inspector.
