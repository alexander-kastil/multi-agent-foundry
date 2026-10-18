import re
import numpy as np
from local_index import embed, open_openai_client

QUERIES = [
    "potassium is below 3.3, do I hold insulin",
    "what infusion rate of insulin in DKA",
]
FIXED_SIZE = 300

text = open("data/dka_protocol.md").read()
title = text.splitlines()[0].lstrip("# ")


def fixed_chunks(text, size):
    return [text[i : i + size] for i in range(0, len(text), size)]


def structural_chunks(text):
    sections = re.split(r"\n(?=## )", text)[1:]
    return [{"heading": s.splitlines()[0].lstrip("# "), "content": "\n".join(s.splitlines()[1:]).strip()} for s in sections]


def top_hit(openai_client, query, chunks):
    vectors = embed(openai_client, chunks)
    vectors = vectors / np.linalg.norm(vectors, axis=1, keepdims=True)
    q = embed(openai_client, [query])[0]
    scores = vectors @ (q / np.linalg.norm(q))
    best = int(np.argmax(scores))
    return best, float(scores[best])


openai_client = open_openai_client()
fixed = fixed_chunks(text, FIXED_SIZE)
sections = structural_chunks(text)
section_texts = [f"{s['heading']}. {s['content']}" for s in sections]
overview = sections[0]["content"]

print(f"Document: {title} ({len(text)} characters)")
print(f"fixed-size chunks: {len(fixed)} of {FIXED_SIZE} characters; structural chunks: {len(sections)} sections")

for query in QUERIES:
    print(f"\nQuery: {query}")
    best, score = top_hit(openai_client, query, fixed)
    print(f"\n  fixed-size  (score {score:.3f}), cuts across section boundaries:")
    print("   ", fixed[best].replace("\n", " ")[:300])
    best, score = top_hit(openai_client, query, section_texts)
    print(f"\n  structural  (score {score:.3f}), one complete section:")
    print("   ", sections[best]["content"].replace("\n", " "))
    print(f"\n  parent-child adds the parent context for '{title}':")
    print("   ", overview[:160], "...")
