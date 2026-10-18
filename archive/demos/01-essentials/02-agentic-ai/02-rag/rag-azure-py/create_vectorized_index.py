import os
from dotenv import load_dotenv

from initialize_clients import initialize_clients
from search_index_manager import SearchIndexManager
from document_processor import retrieve_and_process_documents
from upload_handler import upload_documents
from test_search import test_search_index

load_dotenv()

AZURE_STORAGE_CONNECTION_STRING = os.environ.get('STORAGE_CONNECTION_STRING')
SEARCH_SERVICE_NAME = os.environ.get('SEARCH_SERVICE_NAME')
SEARCH_SERVICE_ENDPOINT = os.environ.get('SEARCH_SERVICE_ENDPOINT')
SEARCH_ADMIN_KEY = os.environ.get('SEARCH_ADMIN_KEY')
AZURE_AI_MODELS_ENDPOINT = os.environ.get('AZURE_AI_MODELS_ENDPOINT')
AZURE_AI_MODELS_KEY = os.environ.get('AZURE_AI_MODELS_KEY')
EMBEDDINGS_MODEL = os.environ.get('EMBEDDINGS_MODEL', 'text-embedding-ada-002')
PROCESSED_CONTAINER = (
    os.environ.get('PROCESSED_CONTAINER')
    or os.environ.get('STORAGE_CONTAINER_NAME')
    or 'processed-documents'
)
PROCESSED_BLOB_NAME = os.environ.get('PROCESSED_BLOB_NAME', 'processed_documents_for_vectorization.json')
SEARCH_INDEX_NAME = 'insurance-documents-index'
CHUNK_SIZE = 1000
CHUNK_OVERLAP = 200


def validate_configuration() -> bool:
    required_settings = {
        'STORAGE_CONNECTION_STRING': AZURE_STORAGE_CONNECTION_STRING,
        'SEARCH_SERVICE_ENDPOINT': SEARCH_SERVICE_ENDPOINT,
        'SEARCH_ADMIN_KEY': SEARCH_ADMIN_KEY,
        'AZURE_AI_MODELS_ENDPOINT': AZURE_AI_MODELS_ENDPOINT,
    }

    missing = [name for name, value in required_settings.items() if not value]
    if missing:
        print("❌ Missing required configuration values:")
        for setting in missing:
            print(f"   - {setting}")
        print("➡️  Update your .env file and retry.")
        return False

    return True


def main():
    print("🚀 Starting Azure AI Search Vectorized Index Creation")
    print("=" * 60)

    if not validate_configuration():
        return
    
    print("\n## 2. Initialize Azure Services")
    blob_service_client, search_index_client, search_client, embeddings_client = initialize_clients(
        storage_connection_string=AZURE_STORAGE_CONNECTION_STRING,
        search_endpoint=SEARCH_SERVICE_ENDPOINT,
        search_admin_key=SEARCH_ADMIN_KEY,
        search_index_name=SEARCH_INDEX_NAME,
        inference_endpoint=AZURE_AI_MODELS_ENDPOINT,
        inference_key=AZURE_AI_MODELS_KEY,
        embeddings_model=EMBEDDINGS_MODEL,
    )
    
    print("\n## 3. Create Azure AI Search Index with Integrated Vectorization")
    index_manager = SearchIndexManager(search_index_client, SEARCH_INDEX_NAME)
    success = index_manager.create_search_index(EMBEDDINGS_MODEL)
    if success:
        print("\n📊 Index created successfully!")
    
    print("\n## 4. Document Retrieval and Processing")
    print("✅ Document processors initialized")
    
    print("\n## 5. Retrieve and Process Documents")
    search_documents = retrieve_and_process_documents(
        blob_service_client=blob_service_client,
        embeddings_client=embeddings_client,
        container_name=PROCESSED_CONTAINER,
        blob_name=PROCESSED_BLOB_NAME,
        chunk_size=CHUNK_SIZE,
        chunk_overlap=CHUNK_OVERLAP,
        embeddings_model=EMBEDDINGS_MODEL
    )

    if not search_documents:
        print("\n❌ No documents prepared for upload. Exiting.")
        return
    
    print("\n## 6. Upload Documents to Azure AI Search with Pre-computed Embeddings")
    upload_success = upload_documents(search_client, search_documents, index_manager)

    if not upload_success:
        print("\n❌ Upload failed. Skipping search validation steps.")
        return
    
    print("\n## 7. Test the Search Index with Semantic and Vector Search")
    print("✅ Search tester initialized")
    
    print("\n## 8. Test with Sample Insurance Queries")
    test_search_index(search_client)
    
    print("\n✅ All steps completed successfully!")


if __name__ == "__main__":
    main()
