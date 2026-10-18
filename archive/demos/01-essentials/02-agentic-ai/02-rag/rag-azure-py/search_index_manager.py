from azure.search.documents.indexes import SearchIndexClient
from azure.search.documents.indexes.models import (
    SearchIndex,
    SearchField,
    SearchFieldDataType,
    SimpleField,
    SearchableField,
    VectorSearch,
    VectorSearchProfile,
    HnswAlgorithmConfiguration,
    ExhaustiveKnnAlgorithmConfiguration,
    SemanticConfiguration,
    SemanticPrioritizedFields,
    SemanticField,
    SemanticSearch,
)
from azure.core.exceptions import ResourceNotFoundError
from typing import Dict


class SearchIndexManager:
    def __init__(self, search_index_client: SearchIndexClient, index_name: str):
        self.search_index_client = search_index_client
        self.index_name = index_name

    def create_search_index(self, embeddings_model: str) -> bool:
        print(f"🚀 Creating search index with manual embeddings using Azure AI Project")
        print(f"🤖 Embeddings model: {embeddings_model}")
        
        vector_search = VectorSearch(
            algorithms=[
                HnswAlgorithmConfiguration(name="insurance-algorithm", kind="hnsw"),
                ExhaustiveKnnAlgorithmConfiguration(name="insurance-eknn", kind="exhaustiveKnn")
            ],
            profiles=[
                VectorSearchProfile(
                    name="insurance-profile",
                    algorithm_configuration_name="insurance-algorithm"
                )
            ]
        )
        
        semantic_config = SemanticConfiguration(
            name="insurance-semantic",
            prioritized_fields=SemanticPrioritizedFields(
                title_field=SemanticField(field_name="title"),
                content_fields=[SemanticField(field_name="content")],
                keywords_fields=[
                    SemanticField(field_name="category"),
                    SemanticField(field_name="file_name")
                ]
            )
        )
        
        semantic_search = SemanticSearch(
            configurations=[semantic_config]
        )
        
        fields = [
            SimpleField(name="id", type=SearchFieldDataType.String, key=True),
            SearchableField(name="title", type=SearchFieldDataType.String),
            SearchableField(name="content", type=SearchFieldDataType.String),
            SearchableField(name="category", type=SearchFieldDataType.String, filterable=True, facetable=True),
            SearchableField(name="file_name", type=SearchFieldDataType.String, filterable=True),
            SimpleField(name="file_type", type=SearchFieldDataType.String, filterable=True),
            SimpleField(name="chunk_id", type=SearchFieldDataType.Int32),
            SimpleField(name="chunk_count", type=SearchFieldDataType.Int32),
            SimpleField(name="original_length", type=SearchFieldDataType.Int32),
            SimpleField(name="chunk_length", type=SearchFieldDataType.Int32),
            SimpleField(name="processing_date", type=SearchFieldDataType.DateTimeOffset),
            SearchField(
                name="content_vector",
                type=SearchFieldDataType.Collection(SearchFieldDataType.Single),
                searchable=True,
                vector_search_dimensions=1536,
                vector_search_profile_name="insurance-profile"
            )
        ]
        
        index = SearchIndex(
            name=self.index_name,
            fields=fields,
            vector_search=vector_search,
            semantic_search=semantic_search
        )
        
        result = self.search_index_client.create_or_update_index(index)
        print(f"✅ Search index '{self.index_name}' created successfully!")
        print(f"📋 Index fields: {len(result.fields)}")
        print(f"🔍 Vector search enabled: {bool(result.vector_search)}")
        print(f"🧠 Semantic search enabled: {bool(result.semantic_search)}")
        print(f"📝 Embeddings will be generated manually using Azure AI Project")
        
        return True
    
    def delete_index_if_exists(self) -> bool:
        try:
            self.search_index_client.delete_index(self.index_name)
            print(f"✅ Deleted existing index: {self.index_name}")
            return True
        except ResourceNotFoundError:
            print(f"ℹ️ Index {self.index_name} doesn't exist - will create new")
            return True
    
    def get_index_stats(self) -> Dict:
        try:
            index = self.search_index_client.get_index(self.index_name)
            stats = self.search_index_client.get_index_statistics(self.index_name)
            
            if hasattr(stats, 'document_count'):
                return {
                    "name": index.name,
                    "field_count": len(index.fields),
                    "document_count": stats.document_count,
                    "storage_size": stats.storage_size,
                    "vector_index_size": getattr(stats, 'vector_index_size', 0)
                }
            else:
                return {
                    "name": index.name,
                    "field_count": len(index.fields),
                    "document_count": stats.get('document_count', 0),
                    "storage_size": stats.get('storage_size', 0),
                    "vector_index_size": stats.get('vector_index_size', 0)
                }
        except Exception as e:
            print(f"❌ Error getting index stats: {e}")
            return {}
