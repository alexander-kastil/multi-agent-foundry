import json
from concurrent.futures import ThreadPoolExecutor
from local_index import LocalIndex, MODEL, load_source, open_openai_client

SOURCES = ["drug_formulary", "clinical_guidelines", "lab_references"]
SOURCE_WEIGHTS = {"drug_formulary": 0.3, "clinical_guidelines": 1.5, "lab_references": 1.0}
HIGH = 0.7
LOW = 0.4

QUERIES = [
    "What are side effects of metformin?",
    "What is the first-line treatment for type 2 diabetes?",
    "What TSH level indicates hypothyroidism?",
    "How do I manage a patient with diabetic ketoacidosis who is hypokalaemic?",
    "Tell me about the Northwind cafeteria opening hours.",
]

CLASSIFICATION_PROMPT = """Analyze this clinical query and give, for each knowledge source, the probability (0 to 1) that it is needed to answer it.

Query: {query}

Knowledge sources:
- drug_formulary: medication information including brand and generic names, dosing, side effects, contraindications, interactions
- clinical_guidelines: diagnosis criteria, treatment algorithms, clinical protocols, disease management
- lab_references: laboratory test reference ranges, interpretation guidelines

Output only a JSON object: {{"drug_formulary": 0.0, "clinical_guidelines": 0.0, "lab_references": 0.0}}"""


def classify(openai_client, query):
    response = openai_client.responses.create(model=MODEL, input=CLASSIFICATION_PROMPT.format(query=query))
    text = response.output_text.strip().removeprefix("```json").removesuffix("```").strip()
    return json.loads(text)


def choose_sources(probabilities, unavailable):
    ranked = sorted(probabilities, key=probabilities.get, reverse=True)
    chosen = [s for s in ranked if probabilities[s] > HIGH]
    if not chosen:
        chosen = [s for s in ranked if probabilities[s] > LOW] if max(probabilities.values()) > LOW else list(ranked)
    available = [s for s in chosen if s not in unavailable]
    return available or [s for s in SOURCES if s not in unavailable]


def normalize(hits):
    scores = [h["score"] for h in hits]
    low, high = min(scores), max(scores)
    return [{**h, "norm": 1.0 if high == low else (h["score"] - low) / (high - low)} for h in hits]


openai_client = open_openai_client()
indexes = {name: LocalIndex(openai_client, load_source(name), name) for name in SOURCES}


def routed_search(query, unavailable=()):
    probabilities = classify(openai_client, query)
    sources = choose_sources(probabilities, unavailable)
    with ThreadPoolExecutor() as pool:
        results = list(pool.map(lambda s: indexes[s].search(query, mode="hybrid", vector_weight=SOURCE_WEIGHTS[s], top=3), sources))
    merged = sorted((h for hits in results for h in normalize(hits)), key=lambda h: -h["norm"])
    return probabilities, sources, merged


total_sources = 0
for query in QUERIES:
    probabilities, sources, merged = routed_search(query)
    total_sources += len(sources)
    print(f"\nQuery: {query}")
    print("  confidence:", {k: round(v, 2) for k, v in probabilities.items()})
    print("  searched  :", sources)
    print("  top results:", " | ".join(f"{h['title']} [{h['source']}]" for h in merged[:3]))
print(f"\naverage sources searched per query: {total_sources / len(QUERIES):.1f} of {len(SOURCES)}")

query = "What are side effects of metformin?"
print(f"\nQuery with drug_formulary unavailable: {query}")
probabilities, sources, merged = routed_search(query, unavailable={"drug_formulary"})
print("  searched  :", sources, "(fallback because the classified source is down)")
print("  top results:", " | ".join(f"{h['title']} [{h['source']}]" for h in merged[:3]))
