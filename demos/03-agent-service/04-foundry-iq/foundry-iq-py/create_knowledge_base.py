import json
from pathlib import Path

from azure.search.documents import SearchClient
from azure.search.documents.indexes import SearchIndexClient
from azure.search.documents.indexes.models import (
    AzureOpenAIVectorizer,
    AzureOpenAIVectorizerParameters,
    HnswAlgorithmConfiguration,
    KnowledgeBase,
    KnowledgeBaseAzureOpenAIModel,
    KnowledgeSourceReference,
    SearchField,
    SearchIndex,
    SearchIndexKnowledgeSource,
    SearchIndexKnowledgeSourceParameters,
    SearchableField,
    SemanticConfiguration,
    SemanticField,
    SemanticPrioritizedFields,
    SemanticSearch,
    SimpleField,
    VectorSearch,
    VectorSearchProfile,
)
from azure.search.documents.knowledgebases.models import KnowledgeRetrievalLowReasoningEffort, KnowledgeRetrievalOutputMode

from common import API_VERSION, EMBEDDING_MODEL, FOUNDRY_ENDPOINT, KNOWLEDGE_BASE, MODEL, SEARCH_ENDPOINT, SOURCES, credential, embeddings

DATA = Path(__file__).parent / "data"
index_client = SearchIndexClient(endpoint=SEARCH_ENDPOINT, credential=credential, api_version=API_VERSION)
embedding_parameters = AzureOpenAIVectorizerParameters(
    resource_url=FOUNDRY_ENDPOINT, deployment_name=EMBEDDING_MODEL, model_name=EMBEDDING_MODEL
)


def build_index(name):
    return SearchIndex(
        name=name,
        fields=[
            SimpleField(name="id", type="Edm.String", key=True),
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
            vectorizers=[AzureOpenAIVectorizer(vectorizer_name="openai", parameters=embedding_parameters)],
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


print("== Step 1: index the documents")
for index_name in SOURCES:
    documents = json.loads((DATA / f"{index_name.removeprefix('m3-')}.json").read_text())
    vectors = embeddings.embeddings.create(model=EMBEDDING_MODEL, input=[d["title"] + ". " + d["content"] for d in documents])
    for document, vector in zip(documents, vectors.data):
        document["content_vector"] = vector.embedding
    index_client.create_or_update_index(build_index(index_name))
    SearchClient(endpoint=SEARCH_ENDPOINT, index_name=index_name, credential=credential).upload_documents(documents)
    print(f"{index_name}: {len(documents)} documents")

print("== Step 2: create knowledge sources and a knowledge base")
for index_name, source_name in SOURCES.items():
    index_client.create_or_update_knowledge_source(
        SearchIndexKnowledgeSource(
            name=source_name,
            description=f"Contoso Outdoor {index_name.removeprefix('m3-')}",
            search_index_parameters=SearchIndexKnowledgeSourceParameters(search_index_name=index_name),
        )
    )
    print(f"knowledge source {source_name} -> index {index_name}")

index_client.create_or_update_knowledge_base(
    KnowledgeBase(
        name=KNOWLEDGE_BASE,
        description="Contoso Outdoor products and store policies",
        retrieval_instructions="Use m3-products-ks for questions about gear, specifications and prices. Use m3-policies-ks for returns, warranty, shipping and repairs. Use both when a question needs both.",
        answer_instructions="Answer briefly and cite the documents you used.",
        output_mode=KnowledgeRetrievalOutputMode.ANSWER_SYNTHESIS,
        knowledge_sources=[KnowledgeSourceReference(name=name) for name in SOURCES.values()],
        models=[
            KnowledgeBaseAzureOpenAIModel(
                azure_open_ai_parameters=AzureOpenAIVectorizerParameters(
                    resource_url=FOUNDRY_ENDPOINT, deployment_name=MODEL, model_name=MODEL
                )
            )
        ],
        retrieval_reasoning_effort=KnowledgeRetrievalLowReasoningEffort(),
    )
)
print(f"knowledge base {KNOWLEDGE_BASE} created with default reasoning effort low")
