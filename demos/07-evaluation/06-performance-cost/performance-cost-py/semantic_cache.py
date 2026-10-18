import hashlib
import json
import os
import re
from datetime import datetime, timezone
import numpy as np

LOCAL_EMBEDDING_DIMENSIONS = 512
WEEK_SECONDS = 7 * 24 * 60 * 60


def create_redis():
    if os.getenv("REDIS_HOST"):
        import redis
        return redis.Redis(
            host=os.environ["REDIS_HOST"],
            port=int(os.getenv("REDIS_PORT", "6380")),
            password=os.getenv("REDIS_KEY"),
            ssl=True,
            decode_responses=True,
        )
    import fakeredis
    return fakeredis.FakeRedis(decode_responses=True)


class SemanticCache:
    def __init__(self, redis_client, openai_client, threshold=None):
        self.redis_client = redis_client
        self.openai_client = openai_client
        self.threshold = threshold or float(os.getenv("CACHE_THRESHOLD", "0.92"))
        self.embedding_deployment = os.getenv("EMBEDDING_DEPLOYMENT_NAME")
        self.hits = 0
        self.misses = 0

    def get_embedding(self, text):
        if self.embedding_deployment:
            response = self.openai_client.embeddings.create(input=text, model=self.embedding_deployment)
            return np.array(response.data[0].embedding)
        vector = np.zeros(LOCAL_EMBEDDING_DIMENSIONS)
        tokens = re.findall(r"[a-z0-9]+", text.lower())
        for token in tokens:
            vector[int(hashlib.md5(token.encode()).hexdigest(), 16) % LOCAL_EMBEDDING_DIMENSIONS] += 1
        return vector

    @staticmethod
    def cosine_similarity(first, second):
        denominator = np.linalg.norm(first) * np.linalg.norm(second)
        return 0.0 if denominator == 0 else float(np.dot(first, second) / denominator)

    def check_cache(self, query_text, agent_id):
        query_embedding = self.get_embedding(query_text)
        best_match = None
        best_similarity = 0.0
        for key in self.redis_client.keys(f"cache:{agent_id}:*")[:500]:
            cached = self.redis_client.hgetall(key)
            if not cached:
                continue
            cached_embedding = np.array([float(value) for value in cached["embedding"].split(",")])
            similarity = self.cosine_similarity(query_embedding, cached_embedding)
            if similarity > best_similarity:
                best_similarity = similarity
                best_match = cached
        if best_match is not None and best_similarity >= self.threshold:
            self.hits += 1
            return {"hit": True, "response": best_match["response"], "similarity": best_similarity}
        self.misses += 1
        return {"hit": False, "similarity": best_similarity}

    def store_cache(self, query_text, response, agent_id, dependencies=None, ttl_seconds=WEEK_SECONDS):
        key = f"cache:{agent_id}:{hashlib.sha256(query_text.encode()).hexdigest()[:16]}"
        self.redis_client.hset(
            key,
            mapping={
                "embedding": ",".join(str(value) for value in self.get_embedding(query_text)),
                "response": response,
                "agent_id": agent_id,
                "timestamp": datetime.now(timezone.utc).isoformat(),
                "dependencies": ",".join(dependencies or []),
            },
        )
        self.redis_client.expire(key, ttl_seconds)

    def invalidate_by_dependency(self, dependency_id):
        removed = 0
        for key in self.redis_client.keys("cache:*") + self.redis_client.keys("result:*"):
            fields = self.redis_client.hgetall(key)
            if dependency_id in fields.get("dependencies", "").split(","):
                self.redis_client.delete(key)
                removed += 1
        return removed

    @property
    def hit_rate(self):
        total = self.hits + self.misses
        return self.hits / total if total else 0.0


class ResultCache:
    def __init__(self, redis_client):
        self.redis_client = redis_client
        self.hits = 0
        self.misses = 0

    @staticmethod
    def key(agent_id, input_signature):
        normalized = json.dumps(input_signature, sort_keys=True).lower()
        return f"result:{agent_id}:{hashlib.sha256(normalized.encode()).hexdigest()[:16]}"

    def get(self, agent_id, input_signature):
        cached = self.redis_client.hgetall(self.key(agent_id, input_signature))
        if cached:
            self.hits += 1
            return cached["response"]
        self.misses += 1
        return None

    def put(self, agent_id, input_signature, response, dependencies=None, ttl_seconds=86400):
        key = self.key(agent_id, input_signature)
        self.redis_client.hset(key, mapping={"response": response, "dependencies": ",".join(dependencies or [])})
        self.redis_client.expire(key, ttl_seconds)
