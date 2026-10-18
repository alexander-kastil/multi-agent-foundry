from typing import Dict, List

from azure.core.exceptions import HttpResponseError
from azure.search.documents import SearchClient


class SearchTester:
    def __init__(self, search_client: SearchClient):
        self.search_client = search_client
    
    def vector_search(self, query: str, top_k: int = 5, category_filter: str = None) -> List[Dict]:
        search_params = {
            "search_text": query,
            "top": top_k,
            "search_mode": "any",
            "query_type": "semantic",
            "semantic_configuration_name": "insurance-semantic",
            "select": ["id", "title", "content", "category", "file_name", "chunk_id", "chunk_count"]
        }
        
        if category_filter:
            search_params["filter"] = f"category eq '{category_filter}'"
        
        try:
            raw_results = self.search_client.search(**search_params)
            hits = list(raw_results)
        except HttpResponseError as error:
            if "Semantic search is not enabled" in str(error):
                fallback_params = search_params.copy()
                fallback_params.pop("semantic_configuration_name", None)
                fallback_params["query_type"] = "simple"
                raw_results = self.search_client.search(**fallback_params)
                hits = list(raw_results)
            else:
                raise

        search_results = []
        for result in hits:
            search_results.append({
                'id': result['id'],
                'title': result['title'],
                'content': result['content'],
                'category': result['category'],
                'file_name': result['file_name'],
                'chunk_id': result['chunk_id'],
                'chunk_count': result['chunk_count'],
                'score': result.get('@search.score', 0),
                'reranker_score': result.get('@search.reranker_score', 0)
            })
        
        return search_results
    
    def display_search_results(self, query: str, results: List[Dict], search_type: str = "Search"):
        print(f"\n🔍 {search_type} Results for: '{query}'")
        print("=" * 80)
        
        if not results:
            print("No results found.")
            return
        
        for i, result in enumerate(results, 1):
            score = float(result.get('score') or 0)
            reranker_score = result.get('reranker_score')

            print(f"\n{i}. 📄 {result['title']}")
            print(f"   📂 Category: {result['category']}")
            print(f"   📊 Score: {score:.4f}", end="")
            if isinstance(reranker_score, (int, float)) and reranker_score > 0:
                print(f" | Reranker: {float(reranker_score):.4f}")
            else:
                print()
            print(f"   📝 Chunk {result['chunk_id'] + 1}")
            
            preview = result['content'][:300]
            if len(result['content']) > 300:
                preview += "..."
            print(f"   💬 Preview: {preview}")
            print("-" * 80)
