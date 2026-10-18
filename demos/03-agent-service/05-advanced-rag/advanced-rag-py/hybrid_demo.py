from hybrid_demo_data import LABELED, RERANK_QUERY
from local_index import LocalIndex, MODEL, load_source, open_openai_client

SCENE_QUERIES = [
    ("clinical_guidelines", "heart attack"),
    ("drug_formulary", "NDC 00071-0156-23"),
    ("drug_formulary", "atorvastatin contraindications"),
    ("clinical_guidelines", "breathlessness with pain on breathing in"),
]

WEIGHTS = [0.3, 0.7, 1.0, 1.5]
SOURCE_WEIGHTS = {"drug_formulary": 0.3, "clinical_guidelines": 1.5}

RERANK_PROMPT = """Rate how well this document answers the query on a scale of 1-10.

Query: {query}

Document Title: {title}
Document Content: {content}

Provide only a number between 1-10, where 10 means the document directly and completely answers the query."""


def reciprocal_rank(index, query, expected, **kwargs):
    ids = [d["id"] for d in index.search(query, top=10, **kwargs)]
    return 1 / (ids.index(expected) + 1) if expected in ids else 0.0


openai_client = open_openai_client()
indexes = {name: LocalIndex(openai_client, load_source(name), name) for name in ("drug_formulary", "clinical_guidelines")}

print("=== 1. Keyword vs vector vs hybrid (hybrid uses the per-source weights tuned in scene 2) ===")
for source, query in SCENE_QUERIES:
    print(f"\n[{source}] {query}")
    for mode in ("keyword", "vector", "hybrid"):
        hits = indexes[source].search(query, mode=mode, vector_weight=SOURCE_WEIGHTS[source], top=3)
        print(f"  {mode:<8}", " | ".join(h["title"] for h in hits) or "no results")

print("\n=== 2. Tune the vector weight per knowledge source (MRR over labeled queries) ===")
print(f"{'source':<22}" + "".join(f"w={w:<6}" for w in WEIGHTS) + "keyword  vector")
for source, pairs in LABELED.items():
    row = []
    for w in WEIGHTS:
        row.append(sum(reciprocal_rank(indexes[source], q, e, mode="hybrid", vector_weight=w) for q, e in pairs) / len(pairs))
    keyword = sum(reciprocal_rank(indexes[source], q, e, mode="keyword") for q, e in pairs) / len(pairs)
    vector = sum(reciprocal_rank(indexes[source], q, e, mode="vector") for q, e in pairs) / len(pairs)
    print(f"{source:<22}" + "".join(f"{v:<8.2f}" for v in row) + f"{keyword:<9.2f}{vector:.2f}")

print("\n=== 3. LLM-as-reranker on the hybrid candidates ===")
print(f"Query: {RERANK_QUERY}")
candidates = []
for index in indexes.values():
    candidates += index.search(RERANK_QUERY, mode="hybrid", top=4)
candidates.sort(key=lambda d: -d["score"])
print("hybrid order :", " | ".join(c["title"] for c in candidates[:5]))
for candidate in candidates[:5]:
    response = openai_client.responses.create(
        model=MODEL,
        input=RERANK_PROMPT.format(query=RERANK_QUERY, title=candidate["title"], content=candidate["content"]),
    )
    candidate["rerank_score"] = float(response.output_text.strip())
reranked = sorted(candidates[:5], key=lambda d: -d["rerank_score"])
print("reranked top3:", " | ".join(f"{c['title']} ({c['rerank_score']:.0f})" for c in reranked[:3]))
