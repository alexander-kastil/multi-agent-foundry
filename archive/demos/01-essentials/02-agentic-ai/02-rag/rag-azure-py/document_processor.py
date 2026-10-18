import uuid
from datetime import datetime
from typing import List, Dict
from tqdm import tqdm
from document_retriever import DocumentRetriever
from text_chunker import TextChunker


def retrieve_and_process_documents(
    blob_service_client,
    embeddings_client,
    container_name: str,
    blob_name: str,
    chunk_size: int,
    chunk_overlap: int,
    embeddings_model: str
) -> List[Dict]:
    retriever = DocumentRetriever(blob_service_client, container_name, blob_name)
    chunker = TextChunker(
        chunk_size=chunk_size,
        chunk_overlap=chunk_overlap
    )
    
    print("\n" + "="*60)
    print("📥 RETRIEVING PROCESSED DOCUMENTS FROM BLOB STORAGE")
    print("="*60)
    
    processed_documents = retriever.get_all_processed_documents()
    if not processed_documents:
        print("❌ No processed documents available. Ensure the preprocessing notebook has been run.")
        return []
    
    print(f"\n🎉 SUCCESS! Retrieved processed documents from blob storage")
    print(f"📊 Available categories: {list(processed_documents.keys())}")
    
    policies_only = {'policies': processed_documents.get('policies', [])}
    if not policies_only['policies']:
        print("❌ No policy documents were found in the processed dataset.")
        return []
    
    print(f"🎯 Filtering to process POLICIES only...")
    print(f"📄 Found {len(policies_only['policies'])} policy documents")
    
    search_documents = []
    
    for category, docs in policies_only.items():
        print(f"\n📂 Processing {category} documents...")
        
        successful_docs = [doc for doc in docs if doc.get('success', False)]
        print(f"✅ Processing {len(successful_docs)} successful {category} documents")
        
        for doc in tqdm(successful_docs, desc=f"Processing {category}"):
            text_content = doc.get('text', '')
            if not text_content:
                print(f"⚠️ Skipping document with no text content: {doc.get('metadata', {}).get('file_name', 'Unknown')}")
                continue
            
            metadata = doc.get('metadata', {}).copy()
            metadata['category'] = category
            
            chunks = chunker.chunk_text_for_search(text_content, metadata)
            
            for chunk in chunks:
                try:
                    embedding_response = embeddings_client.embed(
                        input=[chunk['content']],
                        model=embeddings_model,
                    )
                    content_vector = embedding_response.data[0].embedding
                except Exception as error:  # noqa: BLE001
                    print(
                        f"⚠️ Failed to generate embedding for chunk {chunk['chunk_id']} "
                        f"of {metadata.get('file_name', 'Unknown')}: {error}"
                    )
                    continue
                
                search_doc = {
                    'id': str(uuid.uuid4()),
                    'title': f"{metadata.get('file_name', 'Unknown')} - Part {chunk['chunk_id'] + 1}",
                    'content': chunk['content'],
                    'category': category,
                    'file_name': metadata.get('file_name', 'Unknown'),
                    'file_type': metadata.get('file_type', 'markdown'),
                    'chunk_id': chunk['chunk_id'],
                    'chunk_count': chunk['chunk_count'],
                    'original_length': len(text_content),
                    'chunk_length': len(chunk['content']),
                    'processing_date': datetime.now().isoformat() + 'Z',
                    'content_vector': content_vector
                }
                search_documents.append(search_doc)
    
    print(f"\n✅ Prepared {len(search_documents)} policy document chunks for search index")
    
    if search_documents:
        total_files = len(set(doc['file_name'] for doc in search_documents))
        total_chunks = len(search_documents)
        avg_chunk_length = sum(doc['chunk_length'] for doc in search_documents) / total_chunks
        
        print(f"\n📊 POLICIES INDEXING SUMMARY:")
        print(f"   📄 Total policy files: {total_files}")
        print(f"   🗂️ Total chunks created: {total_chunks}")
        print(f"   📏 Average chunk length: {avg_chunk_length:.0f} characters")
        
        file_stats = {}
        for doc in search_documents:
            file_name = doc['file_name']
            if file_name not in file_stats:
                file_stats[file_name] = 0
            file_stats[file_name] += 1
        
        print(f"\n📋 Policy files breakdown:")
        for file_name, chunk_count in file_stats.items():
            print(f"   • {file_name}: {chunk_count} chunks")
    
    return search_documents
