from azure.search.documents import SearchClient
from typing import List, Dict
from tqdm import tqdm


class SearchIndexUploader:
    def __init__(self, search_client: SearchClient):
        self.search_client = search_client
    
    def upload_documents_batch(self, documents: List[Dict], batch_size: int = 50) -> bool:
        total_docs = len(documents)
        print(f"📤 Uploading {total_docs} documents to search index...")
        
        for i in tqdm(range(0, total_docs, batch_size), desc="Uploading batches"):
            batch = documents[i:i + batch_size]
            
            upload_batch = []
            for doc in batch:
                search_doc = doc.copy()
                upload_batch.append(search_doc)
            
            result = self.search_client.upload_documents(documents=upload_batch)
            
            failed_docs = [r for r in result if not r.succeeded]
            if failed_docs:
                print(f"⚠️ Failed to upload {len(failed_docs)} documents in batch {i//batch_size + 1}")
                for failed in failed_docs[:3]:
                    print(f"   Error: {failed.error_message}")
        
        print(f"✅ Document upload completed!")
        return True
    
    def get_document_count(self) -> int:
        results = self.search_client.search("*", include_total_count=True, top=1)
        return results.get_count()
