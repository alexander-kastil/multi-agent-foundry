---
slide: "05-02"
topic: 05-advanced-rag
title: "Reranking with the semantic ranker"
subtitle: "The top 50 hybrid results rescored for one monitoring question"
layout: diagram
visual-weight: 2/3
visual-type: svg
media-source: opus-svg
media-file: demos/03-agent-service/pptx/images/05-02-semantic-reranking.svg
order: 14
source: demos/03-agent-service/05-advanced-rag/demo-advanced-rag.md
gamma_prompt: >
  Slide titled "Reranking with the semantic ranker", subtitle "The top 50 hybrid results rescored
  for one monitoring question". Four short bullets on the left, a large diagram on the right: the
  hybrid result order for a clinical question on the left, the semantic ranker order with scores
  from 0 to 4 on the right, and lines connecting each document's two positions, three of them
  rising from candidates ranked 6 to 50. White background, grey hybrid list, green ranked list,
  amber lines for the documents that rise.
---

# Reranking with the semantic ranker

## Content

- **Retrieval** casts a wide net, and the reranker orders what it caught
- The **semantic ranker** rescores the top 50 results of a hybrid query
- `@search.rerankerScore` runs from 0 to 4, separate from the RRF score
- Serum potassium and kidney function rise from below the hybrid top 5

## Speaker Notes

Step 6 of the demo asks what to monitor after starting an ACE inhibitor in a patient with diabetes, once with plain hybrid search and once with the semantic ranker. The lines show where each document moves.

Wide net, then order: retrieval decides which documents become candidates, and the reranker reorders only those. A document that retrieval never returned cannot come back.

Semantic ranker: Azure AI Search runs a model-based second stage over the top 50 results of a keyword or hybrid query. You turn it on with `query_type="semantic"` and a semantic configuration on the index.

Reranker score: `@search.rerankerScore` uses a scale of 0 to 4 and is separate from the RRF score. Here Lisinopril 10 mg tablets leads with 2.48.

Deeper candidates rise: Serum potassium, Estimated glomerular filtration rate and the ketoacidosis protocol were outside the hybrid top 5, and the ranker lifts them toward the drug label. Across the labeled queries the guidelines MRR went from 0.58 to 0.92.
