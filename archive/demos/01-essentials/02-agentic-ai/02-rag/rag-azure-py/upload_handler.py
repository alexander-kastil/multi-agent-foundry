import time
from typing import List, Dict
from search_index_uploader import SearchIndexUploader
from search_index_manager import SearchIndexManager


def upload_documents(
    search_client,
    search_documents: List[Dict],
    index_manager: SearchIndexManager
) -> bool:
    uploader = SearchIndexUploader(search_client)
    
    print("\n🚀 Starting POLICIES upload to Azure AI Search...")
    print("=" * 60)
    print("🎯 Uploading POLICY documents only")
    
    success = uploader.upload_documents_batch(search_documents)
    
    if success:
        print("\n⏳ Waiting for indexing to complete...")
        time.sleep(10)
        
        doc_count = uploader.get_document_count()
        print(f"✅ Index now contains {doc_count} policy document chunks")
        
        stats = index_manager.get_index_stats()
        if stats:
            print(f"📊 Index statistics:")
            print(f"   - Policy documents: {stats.get('document_count', 'N/A')}")
            print(f"   - Storage size: {stats.get('storage_size', 'N/A')} bytes")
            print(f"   - Vector index size: {stats.get('vector_index_size', 'N/A')} bytes")
        
        print(f"\n🎯 SUCCESS: Only policy documents have been indexed!")
        print(f"📄 Your Azure AI Search index now contains comprehensive policy information")
        print(f"🔍 Ready for policy-related queries and AI agent integration")
    
    return success
