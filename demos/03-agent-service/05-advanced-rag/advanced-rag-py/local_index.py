import hashlib
import json
import math
import os
import re
import numpy as np
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential, get_bearer_token_provider
from openai import OpenAI

load_dotenv()
MODEL = os.getenv("MODEL_DEPLOYMENT_NAME")
EMBEDDING_MODEL = os.getenv("EMBEDDING_MODEL_DEPLOYMENT_NAME")
CACHE_PATH = ".cache/embeddings.json"
RRF_K = 60


def open_openai_client():
    credential = DefaultAzureCredential(exclude_environment_credential=True, exclude_managed_identity_credential=True)
    resource_url = os.getenv("PROJECT_ENDPOINT").split("/api/projects")[0]
    return OpenAI(
        base_url=f"{resource_url}/openai/v1/",
        api_key=get_bearer_token_provider(credential, "https://ai.azure.com/.default"),
    )


def load_source(name):
    return json.load(open(f"data/{name}.json"))


def embed(openai_client, texts):
    cache = json.load(open(CACHE_PATH)) if os.path.exists(CACHE_PATH) else {}
    keys = [hashlib.sha256(f"{EMBEDDING_MODEL}:{t}".encode()).hexdigest() for t in texts]
    missing = [(k, t) for k, t in zip(keys, texts) if k not in cache]
    if missing:
        response = openai_client.embeddings.create(model=EMBEDDING_MODEL, input=[t for _, t in missing])
        for (key, _), item in zip(missing, response.data):
            cache[key] = item.embedding
        os.makedirs(".cache", exist_ok=True)
        json.dump(cache, open(CACHE_PATH, "w"))
    return np.array([cache[k] for k in keys])


STOPWORDS = {"a", "an", "and", "in", "of", "on", "or", "the", "to", "with", "for", "is", "are", "after", "what", "should", "i", "ndc"}


def tokenize(text):
    return [t for t in re.findall(r"[a-z0-9]+(?:[-.][a-z0-9]+)*", text.lower()) if t not in STOPWORDS]


class LocalIndex:
    def __init__(self, openai_client, docs, name):
        self.openai_client = openai_client
        self.name = name
        self.docs = docs
        self.tokens = [tokenize(d["title"] + " " + d["content"]) for d in docs]
        self.avg_len = sum(len(t) for t in self.tokens) / len(self.tokens)
        self.doc_freq = {}
        for tokens in self.tokens:
            for term in set(tokens):
                self.doc_freq[term] = self.doc_freq.get(term, 0) + 1
        vectors = embed(openai_client, [d["title"] + ". " + d["content"] for d in docs])
        self.vectors = vectors / np.linalg.norm(vectors, axis=1, keepdims=True)

    def bm25_scores(self, query, k1=1.5, b=0.75):
        n = len(self.docs)
        scores = np.zeros(n)
        for term in tokenize(query):
            if term not in self.doc_freq:
                continue
            idf = math.log(1 + (n - self.doc_freq[term] + 0.5) / (self.doc_freq[term] + 0.5))
            for i, tokens in enumerate(self.tokens):
                tf = tokens.count(term)
                if tf:
                    scores[i] += idf * tf * (k1 + 1) / (tf + k1 * (1 - b + b * len(tokens) / self.avg_len))
        return scores

    def vector_scores(self, query):
        q = embed(self.openai_client, [query])[0]
        return self.vectors @ (q / np.linalg.norm(q))

    def search(self, query, mode="hybrid", vector_weight=1.0, top=3):
        bm25 = self.bm25_scores(query)
        vec = self.vector_scores(query)
        if mode == "keyword":
            order = [i for i in np.argsort(-bm25) if bm25[i] > 0]
            scores = {i: bm25[i] for i in order}
        elif mode == "vector":
            order = list(np.argsort(-vec))
            scores = {i: vec[i] for i in order}
        else:
            scores = {}
            for rank, i in enumerate([i for i in np.argsort(-bm25) if bm25[i] > 0]):
                scores[i] = scores.get(i, 0) + 1 / (RRF_K + rank + 1)
            for rank, i in enumerate(np.argsort(-vec)):
                scores[i] = scores.get(i, 0) + vector_weight / (RRF_K + rank + 1)
            order = sorted(scores, key=lambda i: -scores[i])
        return [{**self.docs[i], "score": float(scores[i]), "source": self.name} for i in order[:top]]
