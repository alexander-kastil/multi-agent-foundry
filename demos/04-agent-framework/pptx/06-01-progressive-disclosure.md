---
slide: "06-01"
topic: 06-agent-skills
title: "Progressive disclosure"
subtitle: "What the hut-booking skill puts into context, stage by stage"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/06-01-progressive-disclosure.svg
order: 14
source: demos/04-agent-framework/06-agent-skills/readme.md
gamma_prompt: >
  Slide titled "Progressive disclosure", subtitle "What the hut-booking skill puts into context, stage by
  stage". Two columns. Left column: four short bullets. Right column, wider than the left: the provided diagram
  of four skill folder parts feeding four numbered loading stages, shown as an inline content image at its
  natural 16:9 aspect ratio, scaled to fit the column, never cropped, never used as a background or side
  accent. Use exactly the one image given for this card; do not generate or add any other images, icons or
  logos.
---

# Progressive disclosure

## Content

- **Advertise**: name and description go into the prompt on every run, about 100 tokens per skill
- **Load**: `load_skill` brings the full `SKILL.md` body, ideally under 5000 tokens
- **Read resources**: `read_skill_resource` fetches a reference or asset file only when needed
- **Run scripts**: `run_skill_script` executes a bundled script such as `price_quote.py`

## Speaker Notes

This slide follows the hut-booking skill folder through the four stages an agent works through. Each stage costs tokens only when the task needs it.

Advertise: the name and description from the frontmatter go into the prompt on every run, roughly 100 tokens per skill. Write the description as a trigger, because it is the only text the agent sees before it decides to load the skill.

Load: the agent calls load_skill to retrieve the full SKILL.md body with the booking procedure. Keep that body under 5000 tokens and move reference material into separate files.

Read resources: read_skill_resource fetches CANCELLATION_POLICY.md or the confirmation template only when the procedure needs it. The tool appears only when at least one skill has resources.

Run scripts: run_skill_script executes price_quote.py, which returned total=624 euro for 3 nights, 4 hikers in winter. All three skill tools require approval by default, because skill instructions enter the context and scripts execute code.
