---
slide: "02-01"
topic: 02-prompt-injection
title: "Prompt injection defense layers"
subtitle: "What each layer adds around the model call"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/08-governance/pptx/images/02-01-defense-layers.svg
order: 5
source: demos/08-governance/02-prompt-injection/readme.md
gamma_prompt: >
  Slide titled "Prompt injection defense layers", subtitle "What each layer adds
  around the model call". Left half: four short bullets on the platform filter, the
  baseline prompt, delimiters with a hardened prompt, and the input scan with the
  output check. Right half: a diagram with three horizontal lanes, Layer 1, Layer 2
  and Layer 3, each a left-to-right pipeline; the components a layer adds are amber,
  platform services are lavender. Clean minimal style, white background, show the
  whole diagram without cropping.
---

# Prompt injection defense layers

## Content

- The **platform filter** with Prompt Shields screens user input in every layer
- Layer 1 sends the document with the one-sentence **baseline prompt**
- Layer 2 adds **delimiters** and the hardened prompt `v1.1.0.txt`
- Layer 3 adds an **input scan** before the call and an output check after it

## Speaker Notes

The topic stacks defenses around a clinical summary agent and measures what each one adds. Each row on the slide is one run of the same attack corpus.

Platform filter: the deployment's guardrail screens the user input before any prompt defense runs. A blocked request becomes a visible block marker instead of a crash.

Baseline prompt: layer 1 shows which cases the model already resists on its own. It is the yardstick for the next layers, not a straw man.

Delimiters: `wrap_document` puts the document inside a `<patient-document>` tag, and the hardened prompt says that everything inside the tag is data. It also tells the model what to do when the data contains instructions.

Input scan: a pattern hit appends an elevated safety note to the instructions, and `validate_output` looks for prompt leakage, followed document instructions and replies that run long. Pattern checks only know the phrases you wrote down.
