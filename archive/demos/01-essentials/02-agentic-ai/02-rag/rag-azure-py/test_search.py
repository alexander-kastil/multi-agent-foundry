from search_tester import SearchTester


def test_search_index(search_client):
    search_tester = SearchTester(search_client)
    
    test_queries = [
        "What is covered under collision insurance?",
        "How much does comprehensive coverage cost?", 
        "What are the liability limits for commercial vehicles?",
        "Does my policy cover theft and vandalism?",
        "What happens if I hit an uninsured driver?",
        "High value vehicle insurance requirements",
        "Motorcycle insurance coverage options"
    ]
    
    print("🧪 Testing Azure AI Search with integrated vectorization...")
    print("=" * 80)
    
    for query in test_queries:
        print(f"\n\n🔍 Testing query: '{query}'")
        
        results = search_tester.vector_search(query, top_k=3)
        
        if results:
            print(f"✅ Found {len(results)} relevant chunks")
            search_tester.display_search_results(query, results, "Semantic Search")
        else:
            print("❌ No relevant documents found")
        
        print("-" * 40)
    
    print("\n✅ Query testing completed!")
