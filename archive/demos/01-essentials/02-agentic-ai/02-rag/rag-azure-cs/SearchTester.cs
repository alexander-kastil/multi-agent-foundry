using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;

namespace RagAzure;

public class SearchTester(SearchClient searchClient, object embeddingsClient, AzureConfiguration config)
{

    public async Task TestSearchIndexAsync()
    {
        Console.WriteLine("\n## 7. Test the Search Index with Semantic and Vector Search");
        Console.WriteLine("✅ Search tester initialized");

        Console.WriteLine("\n## 8. Test with Sample Insurance Queries");

        string[] testQueries =
        [
            "What is covered under collision insurance?",
            "How much does comprehensive coverage cost?",
            "What are the liability limits for commercial vehicles?",
            "Does my policy cover theft and vandalism?",
            "What happens if I hit an uninsured driver?",
            "High value vehicle insurance requirements",
            "Motorcycle insurance coverage options"
        ];

        Console.WriteLine("🧪 Testing Azure AI Search with integrated vectorization...");
        Console.WriteLine(new string('=', 80));

        foreach (var query in testQueries)
        {
            Console.WriteLine($"\n\n🔍 Testing query: '{query}'");

            var results = await VectorSearchAsync(query, topK: 3);

            if (results.Count > 0)
            {
                Console.WriteLine($"✅ Found {results.Count} relevant chunks");
                DisplaySearchResults(query, results, "Vector Search");
            }
            else
            {
                Console.WriteLine("❌ No relevant documents found");
            }

            Console.WriteLine(new string('-', 40));
        }

        Console.WriteLine("\n✅ Query testing completed!");
    }

    private async Task<List<Azure.Search.Documents.Models.SearchDocument>> VectorSearchAsync(string query, int topK = 3)
    {
        try
        {
            // Generate embedding for the query
            var queryEmbedding = await GenerateEmbeddingAsync(query);

            SearchOptions searchOptions = new()
            {
                Size = topK,
                Select = { "title", "content", "category", "file_name", "chunk_id" }
            };

            // Use vector search
            VectorizedQuery vectorQuery = new(queryEmbedding)
            {
                KNearestNeighborsCount = topK,
                Fields = { "content_vector" }
            };
            searchOptions.VectorSearch = new();
            searchOptions.VectorSearch.Queries.Add(vectorQuery);

            var searchResult = await searchClient.SearchAsync<Azure.Search.Documents.Models.SearchDocument>(null, searchOptions);

            List<Azure.Search.Documents.Models.SearchDocument> results = [];
            await foreach (var result in searchResult.Value.GetResultsAsync())
            {
                results.Add(result.Document);
            }

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Search failed: {ex.Message}");
            return [];
        }
    }

    private async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (embeddingsClient is AzureOpenAIClient openAIClient)
        {
            var embeddingClient = openAIClient.GetEmbeddingClient(config.EmbeddingsModel);
            var result = await embeddingClient.GenerateEmbeddingAsync(text);
            return result.Value.ToFloats().ToArray();
        }
        else if (embeddingsClient is EmbeddingsClient inferenceClient)
        {
            var embeddingsOptions = new EmbeddingsOptions(new[] { text });
            var result = await inferenceClient.EmbedAsync(embeddingsOptions);
            // The Embedding is BinaryData that needs to be deserialized to float[]
            var embeddingData = result.Value.Data[0].Embedding;
            return embeddingData.ToObjectFromJson<float[]>() ?? Array.Empty<float>();
        }
        else
        {
            throw new InvalidOperationException("Unknown embeddings client type");
        }
    }

    private void DisplaySearchResults(string query, List<Azure.Search.Documents.Models.SearchDocument> results, string searchType)
    {
        Console.WriteLine($"\n{searchType} Results:");
        Console.WriteLine(new string('=', 80));

        for (int i = 0; i < results.Count; i++)
        {
            var result = results[i];
            Console.WriteLine($"\nResult {i + 1}:");
            Console.WriteLine($"  Title: {result.GetString("title")}");
            Console.WriteLine($"  Category: {result.GetString("category")}");
            Console.WriteLine($"  File: {result.GetString("file_name")}");
            Console.WriteLine($"  Chunk: {result.GetInt32("chunk_id")}");

            var content = result.GetString("content");
            var preview = content.Length > 200 ? content.Substring(0, 200) + "..." : content;
            Console.WriteLine($"  Content: {preview}");
        }
    }
}
