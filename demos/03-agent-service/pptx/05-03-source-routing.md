---
slide: "05-03"
topic: 05-advanced-rag
title: "Routing questions to knowledge sources"
subtitle: "Model confidence per source decides which indexes are searched"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/05-03-source-routing.svg
order: 15
source: demos/03-agent-service/05-advanced-rag/demo-advanced-rag.md
gamma_prompt: >
  Slide titled "Routing questions to knowledge sources", subtitle "Model confidence per source
  decides which indexes are searched". Four short bullets on the left, a large horizontal bar
  chart on the right: three clinical questions, each with a confidence bar for the drug formulary,
  the clinical guidelines and the lab references, dashed threshold lines at 0.4 and 0.7, and a
  green searched marker on every source the router queries. White background, green bars for
  searched sources, amber threshold lines.
---

# Routing questions to knowledge sources

## Content

- A router asks the model for the **probability** that each source is needed
- Sources above **0.7** are searched in parallel
- If none qualifies, the router **widens** to sources above 0.4, then to all sources
- The five demo questions searched 1.6 of 3 sources on average

## Speaker Notes

Step 4 routes five questions across the drug formulary, the clinical guidelines and the lab references. The chart shows three of them with the confidence the model gave each source.

Probability per source: `routing_demo.py` asks the model how likely each source is needed, which keeps intent apart from source health. The values are model output and change between runs.

Above 0.7: the TSH question scores 0.96 for `lab_references` and searches only that source, even though `clinical_guidelines` reaches 0.44. The ketoacidosis question clears 0.7 on two sources and searches both.

Widen, then all: when no source passes 0.7 the router takes those above 0.4, and when none passes 0.4 it searches everything. The cafeteria question scores 0.0 everywhere, so all three sources are searched.

Fewer calls: the five questions searched 1.6 of 3 sources on average. Scores from different indexes are min-max normalized before the results are merged.
