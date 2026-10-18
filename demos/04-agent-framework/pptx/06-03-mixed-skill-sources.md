---
slide: "06-03"
topic: 06-agent-skills
title: "Mixed skill sources"
subtitle: "File and code-defined skills in one provider, read-only tools approved automatically"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/04-agent-framework/pptx/images/06-03-mixed-skill-sources.svg
order: 16
source: demos/04-agent-framework/06-agent-skills/demo-agent-skills.md
gamma_prompt: >
  Slide titled "Mixed skill sources", subtitle "File and code-defined skills in one provider, read-only tools
  approved automatically". Two columns. Left column: four short bullets. Right column, wider than the left: the
  provided diagram of nested skill sources feeding an agent whose approval middleware passes two skill tools
  and asks for the third, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the
  column, never cropped, never used as a background or side accent. Use exactly the one image given for this
  card; do not generate or add any other images, icons or logos.
---

# Mixed skill sources

## Content

- **FileSkillsSource** reads `hut-booking` and `trail-safety` from disk and runs their scripts through `run_script`
- **InMemorySkillsSource** adds the code-defined `altitude` skill, whose `convert` script runs in your process
- **AggregatingSkillsSource** combines both inside `DeduplicatingSkillsSource` and one `SkillsProvider`
- **read_only_tools_auto_approval_rule** approves `load_skill` and `read_skill_resource`; `run_skill_script` still asks

## Speaker Notes

Step 4 of the demo mixes skills from files with a skill defined in code and stops the approval prompts for the harmless tools. One question then uses two skills.

File skills: FileSkillsSource discovers hut-booking and trail-safety in the skills folder. Without a script_runner, file-based scripts fail when the agent asks to run one.

Code-defined skills: altitude is an InlineSkill whose convert script runs in your process, so it needs no runner. Its arguments arrive as a JSON object, while file-based ones arrive as a list of strings.

Composition: AggregatingSkillsSource combines the two sources, DeduplicatingSkillsSource wraps them, and the provider caches the result. In C# the AgentSkillsProviderBuilder assembles the same pipeline.

Approvals: ToolApprovalMiddleware with the read-only rule approves load_skill and read_skill_resource and still asks for run_skill_script. In the demo, trail-safety and altitude loaded without asking, and only the convert script call reached you.
