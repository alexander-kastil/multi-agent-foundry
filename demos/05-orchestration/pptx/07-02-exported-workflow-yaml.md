---
slide: "07-02"
topic: 07-declarative-workflows
title: "Running an exported Foundry workflow"
subtitle: "Local agents registered under the Foundry names"
layout: diagram
visual-weight: 1/2
visual-type: svg
media-source: opus-svg
media-file: demos/05-orchestration/pptx/images/07-02-exported-workflow-yaml.svg
order: 18
source: demos/05-orchestration/07-declarative-workflows/demo-declarative-workflows.md
gamma_prompt: >
  Slide titled "Running an exported Foundry workflow", subtitle "Local agents registered under the
  Foundry names". Two columns. Left column: the four bullets. Right column, wider than the left: the
  provided diagram of three cards, a purple Foundry project, an amber exported.workflow.yaml and a blue
  Agent Framework card, with the agent names m5-classifier, m5-billing and m5-technical joined by dashed
  lines across all three, above a green band where both runs route three messages to billing, technical
  and other, shown as an inline content image at its natural 16:9 aspect ratio, scaled to fit the column,
  never cropped, never used as a background or side accent. Use exactly the one image given for this
  card; do not generate or add any other images, icons or logos.
---

# Running an exported Foundry workflow

## Content

- `export_workflow.py` saves version 1 of `m5-support-router` as `exported.workflow.yaml`
- The YAML calls agents by name: `m5-classifier`, `m5-billing` and `m5-technical`
- `run_migrated.py` registers local agents under **the same names** and loads the file with `WorkflowFactory`
- Both runs route the three messages to billing, technical and other

## Speaker Notes

Workflows built in the Foundry portal designer retire on December 1, 2026, and the way out is to export the YAML and run it in Agent Framework. This slide follows the migration of the published `m5-support-router` in the demo.

Export: `export_workflow.py` reads the definition back from the published version and saves it as `migration/exported.workflow.yaml`. In the portal you open the YAML view and copy the file instead.

Agent names: every `InvokeAzureAgent` action names an agent. In Foundry those names resolve to the prompt agents that `publish_portal_workflow.py` created.

Local agents: the Python loader resolves names from the agents registered on the factory, so `run_migrated.py` registers local agents under the exact Foundry names. The exported file then runs without edits.

Same routing: both runs send the three messages to billing, technical and other. The output shape differs: Foundry shows the classifier JSON in its response text, while `autoSend: false` hides it in the local run.
