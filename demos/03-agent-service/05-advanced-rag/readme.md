# Advanced RAG: Hybrid Search & Reranking

A retrieval-augmented agent is only as good as the passages it retrieves. Keyword search finds the exact drug code and misses "heart attack" when the document says "myocardial infarction", and vector search does the opposite. This topic builds the techniques that close the gap: hybrid search, per-source weighting, reranking, source routing and chunking.

Foundry IQ from the previous topic packages many of these steps behind a knowledge base. Here you open the box, measure each technique on labeled queries and then run the same retrieval on Azure AI Search.

## Why one search mode is not enough

Keyword (BM25) search matches terms, so it is exact on identifiers and blind to synonyms. Vector search matches meaning, so it handles paraphrases and blurs identifiers. A query for `NDC 00071-0156-23` wants the first; a query for "breathlessness with pain on breathing in" wants the second.

| Query | Keyword | Vector | Why |
| --- | --- | --- | --- |
| `NDC 00071-0156-23` | Finds the exact tablet | Returns unrelated drugs | The code is a token, not a concept |
| `heart attack` | Nothing | Finds the acute coronary pathway | The document never uses the words |

Measure the difference instead of guessing. Mean reciprocal rank (MRR) over a small set of labeled queries gives each mode a single score between 0 and 1.

```python
def reciprocal_rank(index, query, expected, **kwargs):
    ids = [d["id"] for d in index.search(query, top=10, **kwargs)]
    return 1 / (ids.index(expected) + 1) if expected in ids else 0.0
```

## Hybrid search with Reciprocal Rank Fusion

A hybrid query runs the keyword query and the vector query at the same time and merges the two ranked lists with Reciprocal Rank Fusion (RRF). Each result list contributes `1 / (k + rank)` for every document, the contributions are added and the documents are re-sorted. A weight on the vector leg makes it count more or less.

```python
for rank, i in enumerate(keyword_order):
    scores[i] = scores.get(i, 0) + 1 / (RRF_K + rank + 1)
for rank, i in enumerate(vector_order):
    scores[i] = scores.get(i, 0) + vector_weight / (RRF_K + rank + 1)
```

The same model runs inside Azure AI Search. A `VectorizableTextQuery` carries the weight, and the index vectorizer turns the query text into a vector at query time.

```python
from azure.search.documents.models import VectorizableTextQuery

results = search.search(
    search_text=query,
    vector_queries=[VectorizableTextQuery(text=query, k_nearest_neighbors=50, fields="content_vector", weight=1.0)],
    top=10,
)
```

RRF scores look small because each leg contributes roughly `1/k` at most; the order matters, not the magnitude. The best weight differs per knowledge source, which is why you tune it with labeled queries instead of fixing it once.

## Rerank the candidates

Retrieval casts a wide net and reranking orders what it caught. A reranker cannot recover a document that retrieval missed, so reranking only helps when the right document is somewhere in the candidate set.

Azure AI Search offers the semantic ranker, a model-based second stage that rescores the top 50 results of a keyword or hybrid query. It reports `@search.rerankerScore` on a scale of 0 to 4, separate from the RRF score.

```python
results = search.search(
    search_text=query,
    vector_queries=[vector],
    query_type="semantic",
    semantic_configuration_name="default",
    top=5,
)
for result in results:
    print(result["title"], result["@search.reranker_score"])
```

The alternative is an LLM as reranker: ask a model to score each candidate from 1 to 10 against the query. It works with any index and gives you control over the rubric, but it adds one model call per candidate and the scores are not stable between runs. Prefer the semantic ranker for latency and cost, and an LLM reranker when you need a custom rubric.

## Route a question to the right sources

An enterprise has several knowledge sources, and searching all of them for every question wastes calls and adds noise. A router asks a model for the probability that each source is needed, searches only the confident ones and merges the results.

| Rule | Value |
| --- | --- |
| Search a source when its probability is above | 0.7 |
| If none qualifies, widen to sources above | 0.4 |
| If still none, search all sources | all |
| A source marked unavailable | skipped, with a fallback to the rest |

Scores from different indexes are not comparable, so min-max normalize each result list before merging. Routing separates intent (the classifier) from capability (source health), which keeps classification stable when an index is down. A Foundry IQ knowledge base does the same job declaratively with retrieval instructions and knowledge source selection.

## Chunk so rules arrive whole

Chunk boundaries decide whether the rule "if potassium is below 3.3, hold insulin" reaches the model complete or cut in half.

| Strategy | How it splits | Trade-off |
| --- | --- | --- |
| Fixed size | Every N characters | Simple, cuts across sentences and sections |
| Structural | At headings or sections | Keeps a rule together, needs structured input |
| Parent-child | Search small chunks, return the parent section | Precise matching with enough surrounding context |

```python
def structural_chunks(text):
    sections = re.split(r"\n(?=## )", text)[1:]
    return [{"heading": s.splitlines()[0].lstrip("# "), "content": "\n".join(s.splitlines()[1:]).strip()} for s in sections]
```

## Hands-On Demo

[Tune Hybrid Search, Reranking and Routing](demo-advanced-rag.md) measures keyword, vector and hybrid retrieval on a clinical knowledge set, adds an LLM reranker, routes questions across three sources, compares chunking strategies and then runs hybrid search with the semantic ranker on Azure AI Search. The runnable projects are `advanced-rag-py/` (Python) and `advanced-rag-cs/` (C#).

## Key Topics

- [Hybrid search overview](https://learn.microsoft.com/azure/search/hybrid-search-overview)
- [Relevance scoring in hybrid search using RRF](https://learn.microsoft.com/azure/search/hybrid-search-ranking)
- [Semantic ranking in Azure AI Search](https://learn.microsoft.com/azure/search/semantic-search-overview)
- [Create a hybrid query](https://learn.microsoft.com/azure/search/hybrid-search-how-to-query)
- [Information retrieval for RAG](https://learn.microsoft.com/azure/architecture/ai-ml/guide/rag/rag-information-retrieval)
- [Agentic retrieval overview](https://learn.microsoft.com/azure/search/agentic-retrieval-overview)
