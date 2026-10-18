---
slide: "06-02"
topic: 06-agent-memory
title: "Memory scope per user"
subtitle: "How the x-memory-user-id header keeps users apart in one agent version"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/06-02-memory-scope.svg
order: 17
source: demos/03-agent-service/06-agent-memory/demo-agent-memory.md
gamma_prompt: >
  Slide titled "Memory scope per user", subtitle "How the x-memory-user-id header keeps users apart
  in one agent version". Four short bullets on the left, a large diagram on the right: two users
  ask the same coffee question in new conversations, each request carries its own user id header,
  one shared agent version resolves the memory scope from that header, one scope holds a stored
  preference and the other is empty, and the two answers differ. White background, blue users and
  agent, purple memory store, green answer for the remembered preference.
---

# Memory scope per user

## Content

- The memory search tool uses the scope `{{$userId}}`
- Each request names its user in the **`x-memory-user-id`** header
- carol's preference carries over into a new conversation
- dave's scope is empty, so the agent says it remembers nothing

## Speaker Notes

Step 5 has two users ask the same question in brand-new conversations against one agent version. The diagram shows why the answers differ.

Scope placeholder: `MemorySearchPreviewTool` on `m3-memory-agent:1` uses the scope `{{$userId}}`, so one agent version serves every user. Each scope is an isolated collection inside `m3-memory-store`.

Header: the service resolves `{{$userId}}` from the `x-memory-user-id` request header and falls back to the caller's Entra tenant and object id without it. Python passes the header with `extra_headers`, C# with a small `PipelinePolicy` on a client built per user.

Recall: carol mentioned oat milk in a flat white in the first conversation, and the new conversation never contains those words. The answer comes from the memories in scope carol.

Isolation: scope dave holds nothing, so the same agent answers that it has no saved memory about the usual coffee. In Step 3, bob sees none of alice's memories for the same reason.
