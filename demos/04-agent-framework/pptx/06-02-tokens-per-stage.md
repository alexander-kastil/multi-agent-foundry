---
slide: "06-02"
topic: 06-agent-skills
title: "Input tokens per loading stage"
subtitle: "What run_progressive.py measured for one hut quote"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/06-02-tokens-per-stage.svg
order: 15
source: demos/04-agent-framework/06-agent-skills/demo-agent-skills.md
gamma_prompt: >
  Slide titled "Input tokens per loading stage", subtitle "What run_progressive.py measured for one hut quote".
  Two columns. Left column: four short bullets. Right column, wider than the left: the provided bar chart of
  input tokens per model call, from 31 without skills to 1244 on the third call, shown as an inline content
  image at its natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a background
  or side accent. Use exactly the one image given for this card; do not generate or add any other images,
  icons or logos.
---

# Input tokens per loading stage

## Content

- **Without skills**: a greeting costs 31 input tokens
- **Advertise only**: 682 tokens with the skill descriptions and the three skill tool definitions
- **Model call 2**: 990 tokens once the loaded `SKILL.md` joins the context
- **Model call 3**: 1244 tokens with the policy, the template and the script result

## Speaker Notes

This slide puts numbers on progressive disclosure. A chat middleware recorded the input tokens of every model call over three runs of the demo.

Without skills: the plain agent answers "Say hello." with 31 input tokens. That is the baseline with no skill in the prompt.

Advertise only: the same greeting costs 682 tokens once skills are advertised. The number covers the skill descriptions and the three skill tool definitions.

Model call 2: the hut quote run starts at 696 tokens and reaches 990 on the second call. That call adds the loaded SKILL.md.

Model call 3: the third call reaches 1244 tokens, because the policy, the template and the script result are now in the context. A greeting never pays for the policy.
