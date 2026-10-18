---
slide: "04-01"
topic: 04-zero-trust
title: "Key rotation without downtime"
subtitle: "Primary and secondary keys with a fallback"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/04-01-key-rotation.svg
order: 10
source: demos/08-governance/04-zero-trust/demo-zero-trust.md
gamma_prompt: >
  Slide titled "Key rotation without downtime", subtitle "Primary and secondary keys
  with a fallback". Left half: four short bullets, one per rotation step. Right half:
  a grid with four columns (before rotation, write the new secondary, disable the
  primary, restore the primary) and three rows (primary, secondary, what the caller
  reads); changed cells are amber, valid keys green, and the caller row shows a valid
  key in every column. Clean minimal style, white background, show the whole grid
  without cropping.
---

# Key rotation without downtime

## Content

- The vault holds a **primary and a secondary** copy of the key
- Step 1 writes the **new value** to the secondary
- Step 2 disables the primary, and the caller **falls back** to the secondary
- Step 3 **restores the primary** with the new value

## Speaker Notes

When a partner API forces you to hold a key, store two copies and let the consumer fall back. The order of the three steps never leaves a moment without a valid key.

Primary and secondary: `get_current_api_key` returns the primary while it is enabled and reads the secondary otherwise. The demo uses an in-memory vault, and the real Key Vault client has the same shape.

New value: `style-api-key-secondary` gets `key-v2` first, while callers still read `key-v1` from the primary.

Falls back: with the primary disabled, the read during rotation returns `key-v2` from the secondary.

Restores the primary: the primary now holds `key-v2` as well. The caller asked for the current key three times and always received a valid one.
