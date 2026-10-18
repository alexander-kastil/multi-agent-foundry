import json
import os
from pathlib import Path

from azure.identity import DefaultAzureCredential, get_bearer_token_provider
from azure.search.documents import SearchClient
from azure.search.documents.indexes import SearchIndexClient
from azure.search.documents.indexes.models import (
    AzureOpenAIVectorizer,
    AzureOpenAIVectorizerParameters,
    HnswAlgorithmConfiguration,
    SearchField,
    SearchIndex,
    SearchableField,
    SemanticConfiguration,
    SemanticField,
    SemanticPrioritizedFields,
    SemanticSearch,
    SimpleField,
    VectorSearch,
    VectorSearchProfile,
)
from azure.search.documents.models import VectorizableTextQuery
from dotenv import load_dotenv
from openai import OpenAI

from hybrid_demo_data import LABELED, RERANK_QUERY

load_dotenv(override=True)

SEARCH_ENDPOINT = os.environ["SEARCH_ENDPOINT"]
FOUNDRY_ENDPOINT = os.environ["FOUNDRY_ENDPOINT"]
EMBEDDING_MODEL = os.environ["EMBEDDING_MODEL_DEPLOYMENT_NAME"]
INDEX = "m3-health"
SOURCES = ["drug_formulary", "clinical_guidelines", "lab_references"]
VECTOR_WEIGHT = 1.0

credential = DefaultAzureCredential(exclude_environment_credential=True, exclude_managed_identity_credential=True)
embeddings = OpenAI(
    base_url=f"{FOUNDRY_ENDPOINT}/openai/v1/",
    api_key=get_bearer_token_provider(credential, "https://cognitiveservices.azure.com/.default"),
)
index_client = SearchIndexClient(endpoint=SEARCH_ENDPOINT, credential=credential)

print("== Load the documents into Azure AI Search")
index_client.create_or_update_index(
    SearchIndex(
        name=INDEX,
        fields=[
            SimpleField(name="id", type="Edm.String", key=True),
            SimpleField(name="source", type="Edm.String", filterable=True),
            SearchableField(name="title", type="Edm.String"),
            SearchableField(name="content", type="Edm.String"),
            SearchField(
                name="content_vector",
                type="Collection(Edm.Single)",
                searchable=True,
                vector_search_dimensions=1536,
                vector_search_profile_name="vector-profile",
            ),
        ],
        vector_search=VectorSearch(
            algorithms=[HnswAlgorithmConfiguration(name="hnsw")],
            profiles=[VectorSearchProfile(name="vector-profile", algorithm_configuration_name="hnsw", vectorizer_name="openai")],
            vectorizers=[
                AzureOpenAIVectorizer(
                    vectorizer_name="openai",
                    parameters=AzureOpenAIVectorizerParameters(
                        resource_url=FOUNDRY_ENDPOINT, deployment_name=EMBEDDING_MODEL, model_name=EMBEDDING_MODEL
                    ),
                )
            ],
        ),
        semantic_search=SemanticSearch(
            default_configuration_name="default",
            configurations=[
                SemanticConfiguration(
                    name="default",
                    prioritized_fields=SemanticPrioritizedFields(
                        title_field=SemanticField(field_name="title"),
                        content_fields=[SemanticField(field_name="content")],
                    ),
                )
            ],
        ),
    )
)

documents = []
for source in SOURCES:
    documents += [{**d, "source": source} for d in json.loads((Path("data") / f"{source}.json").read_text())]
vectors = embeddings.embeddings.create(model=EMBEDDING_MODEL, input=[d["title"] + ". " + d["content"] for d in documents])
for document, vector in zip(documents, vectors.data):
    document["content_vector"] = vector.embedding
search = SearchClient(endpoint=SEARCH_ENDPOINT, index_name=INDEX, credential=credential)
search.upload_documents(documents)
print(f"{INDEX}: {len(documents)} documents")


def run(query, mode, source=None, top=10):
    arguments = {"top": top, "select": ["id", "title"]}
    if source:
        arguments["filter"] = f"source eq '{source}'"
    vector = VectorizableTextQuery(text=query, k_nearest_neighbors=50, fields="content_vector", weight=VECTOR_WEIGHT)
    if mode == "keyword":
        return list(search.search(search_text=query, **arguments))
    if mode == "vector":
        return list(search.search(search_text=None, vector_queries=[vector], **arguments))
    if mode == "hybrid":
        return list(search.search(search_text=query, vector_queries=[vector], **arguments))
    return list(
        search.search(search_text=query, vector_queries=[vector], query_type="semantic", semantic_configuration_name="default", **arguments)
    )


def reciprocal_rank(query, expected, mode, source):
    ids = [r["id"] for r in run(query, mode, source)]
    return 1 / (ids.index(expected) + 1) if expected in ids else 0.0


print("\n== Mean reciprocal rank over the labeled queries (higher is better) ==")
modes = ["keyword", "vector", "hybrid", "hybrid + semantic"]
print(f"{'source':<22}" + "".join(f"{m:<20}" for m in modes))
for source, pairs in LABELED.items():
    scores = []
    for mode in ("keyword", "vector", "hybrid", "semantic"):
        scores.append(sum(reciprocal_rank(q, e, mode, source) for q, e in pairs) / len(pairs))
    print(f"{source:<22}" + "".join(f"{s:<20.2f}" for s in scores))

print(f"\n== Semantic ranker on: {RERANK_QUERY}")
print("hybrid order  :", " | ".join(r["title"] for r in run(RERANK_QUERY, "hybrid", top=5)))
reranked = run(RERANK_QUERY, "semantic", top=5)
print("semantic order:", " | ".join(f"{r['title']} ({r['@search.reranker_score']:.2f})" for r in reranked))

index_client.delete_index(INDEX)
print(f"\nDeleted index {INDEX}")
