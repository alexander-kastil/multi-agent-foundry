---
slide: "99-01"
topic: 01-foundry-essentials
title: "Takeaways"
subtitle: "What the agent modules build on"
layout: content
visual-weight: 1/3
visual-type: photo
media-source: nano-banana
media-file: demos/01-foundry-essentials/pptx/images/99-01-takeaways.jpg
visual-prompt: >
  ONE single continuous photograph taken with one camera in one moment: no split screen,
  no diptych, no collage. Three software engineers, two women and a man in their late
  twenties to mid thirties in modern work-casual clothes, standing in a bright open studio
  in front of a large wall display that shows softly blurred bar charts and connected
  boxes in pastel colours. One woman gestures at the display while the other two nod and
  smile, one of them holding a coffee cup. The group is centred in the frame, seen from
  the knees up, in a vertical composition. Modern, light, bright and friendly: high-key
  natural daylight, white and light-oak contemporary interior, soft pastel accents in
  light blue, mint and lavender, green plants, 2020s design. Screens show only soft
  blurred shapes and colours, never readable UI. 2:3 portrait, contemporary editorial
  photography. Avoid: readable text, logos, watermarks, extra people, dark or moody
  lighting, vintage or retro decor, grime, clutter.
order: 19
source: demos/01-foundry-essentials/readme.md
gamma_prompt: >
  Closing slide titled "Takeaways", subtitle "What the agent modules build on".
  Side-accent layout: title, subtitle and five bullets on the left on plain white, the
  provided portrait photograph of three engineers in front of a bright wall display
  filling the right side, no text over it. Use exactly the one image given for this card;
  do not generate or add any other images, icons or logos.
---

# Takeaways

## Content

- One **project endpoint** in `PROJECT_ENDPOINT` reaches models, agents, evaluations and tools
- **Model router** picks the model per call; a subset limits the pool and doubles as the failover set
- The **Foundry SDK** reaches the project, and `IChatClient` keeps .NET code provider-neutral
- The **Responses API** keeps history by id, in a conversation or in your code, and streams the answer
- Measure `input_tokens`, then **trim, compact or cache**; operate it all with `azd ai` and the Foundry MCP server

## Speaker Notes

Close with what the agent modules take for granted from here on, one line per topic.

One endpoint: the class projects keep the project endpoint in `PROJECT_ENDPOINT`, and Foundry User at the project scope is all a day-to-day developer needs. The Foundry SDK, the Responses API, agents, evaluations and the Foundry MCP server all start from it.

Model router: the router chooses the model per call within its routing mode. A subset restricts the pool, and with at least two models it is also the failover set.

SDK: the project client hands you the OpenAI-compatible client, so authentication and the endpoint are configured once. Because the agent frameworks in later modules also consume `IChatClient`, the .NET code carries over.

Responses API: chain by `previous_response_id`, keep a conversation, or own the list with `store=False`. Streaming combines with all of them, and a streamed answer is still appended to the conversation.

Context and tooling: read `usage` on every response, trim by turn, compact tool-heavy histories and keep the stable prefix first for the cache. The `azd ai` extensions and the Foundry MCP server operate the project without the portal.
