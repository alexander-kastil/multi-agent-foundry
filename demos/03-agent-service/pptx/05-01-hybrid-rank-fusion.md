---
slide: "05-01"
topic: 05-advanced-rag
title: "Hybrid search with Reciprocal Rank Fusion"
subtitle: "How a keyword hit and a vector list merge into one order"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/05-01-hybrid-rank-fusion.svg
order: 13
source: demos/03-agent-service/05-advanced-rag/demo-advanced-rag.md
gamma_prompt: >
  Slide titled "Hybrid search with Reciprocal Rank Fusion", subtitle "How a keyword hit and a
  vector list merge into one order". Four short bullets on the left, a large diagram on the
  right: a keyword result list and a vector result list for the drug code NDC 00071-0156-23, each
  entry with its 1/(k + rank) contribution, and arrows into a fused hybrid list where the exact
  keyword match stays first. White background, grey lists, amber score chips, green hybrid list.
---

# Hybrid search with Reciprocal Rank Fusion

## Content

- The **keyword leg** finds the exact code: Atorvastatin 40 mg tablets at rank 1
- The **vector leg** ranks by meaning and puts the code match further down
- **RRF** adds `1 / (k + rank)` per leg, with `k = 60` and a weight on the vector leg
- With weight 0.3, keyword rank 1 outweighs any single vector rank, so hybrid keeps the exact match first

## Speaker Notes

Step 1 of the demo runs the formulary code `NDC 00071-0156-23` in three modes. The diagram shows how the hybrid order comes out of the two ranked lists.

Keyword leg: BM25 matches the code as a token and returns one document, Atorvastatin 40 mg tablets. At rank 1 it contributes 1/(60+1), about 0.0164.

Vector leg: cosine search ranks Salbutamol, Insulin glargine and Metformin first and puts Atorvastatin lower. The code is a token, not a concept, so meaning alone loses it.

RRF: each list adds 1/(k + rank) for every document, with the vector leg multiplied by its weight, and the sums are re-sorted. The demo uses k = 60 and a vector weight of 0.3 for the formulary.

Exact match first: Atorvastatin collects both contributions and stays on top, and the vector hits fill ranks 2 and 3. The scores look small because the order matters, not the magnitude.
