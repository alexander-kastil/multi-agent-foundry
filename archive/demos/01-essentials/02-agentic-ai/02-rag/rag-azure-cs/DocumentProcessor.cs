using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.Storage.Blobs;

namespace RagAzure;

public class DocumentProcessor(AzureConfiguration config)
{

    public async Task<List<SearchDocument>> RetrieveAndProcessDocumentsAsync(
        BlobServiceClient blobServiceClient,
        object embeddingsClient,
        string containerName,
        string blobName)
    {
        Console.WriteLine("\n## 4. Document Retrieval and Processing");
        Console.WriteLine("✅ Document processors initialized");

        Console.WriteLine("\n## 5. Retrieve and Process Documents");
        Console.WriteLine(new string('=', 60));
        Console.WriteLine("📥 RETRIEVING PROCESSED DOCUMENTS FROM BLOB STORAGE");
        Console.WriteLine(new string('=', 60));

        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            Console.WriteLine($"❌ Blob not found: {blobName}");
            return [];
        }

        var download = await blobClient.DownloadContentAsync();
        var jsonContent = download.Value.Content.ToString();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var processedDocuments = JsonSerializer.Deserialize<ProcessedDocumentsWrapper>(jsonContent, options);

        if (processedDocuments?.Policies == null || processedDocuments.Policies.Count == 0)
        {
            Console.WriteLine("❌ No policy documents were found in the processed dataset.");
            return [];
        }

        Console.WriteLine($"🎉 SUCCESS! Retrieved processed documents from blob storage");
        Console.WriteLine($"📄 Found {processedDocuments.Policies.Count} policy documents");

        var chunker = new TextChunker(config.ChunkSize, config.ChunkOverlap);
        List<SearchDocument> searchDocuments = [];

        Console.WriteLine($"\n📂 Processing policies documents...");
        var successfulDocs = processedDocuments.Policies.Where(d => d.Success).ToList();
        Console.WriteLine($"✅ Processing {successfulDocs.Count} successful policies documents");

        foreach (var doc in successfulDocs)
        {
            if (string.IsNullOrWhiteSpace(doc.Text))
            {
                Console.WriteLine($"⚠️ Skipping document with no text content: {doc.Metadata?.FileName ?? "Unknown"}");
                continue;
            }

            Dictionary<string, string> metadata = new()
            {
                ["category"] = "policies",
                ["file_name"] = doc.Metadata?.FileName ?? "Unknown",
                ["file_type"] = doc.Metadata?.FileType ?? "markdown"
            };

            var chunks = chunker.ChunkTextForSearch(doc.Text, metadata);

            foreach (var chunk in chunks)
            {
                try
                {
                    var embedding = await GenerateEmbeddingAsync(embeddingsClient, chunk.Content);

                    SearchDocument searchDoc = new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Title = $"{metadata["file_name"]} - Part {chunk.ChunkId + 1}",
                        Content = chunk.Content,
                        Category = "policies",
                        FileName = metadata["file_name"],
                        FileType = metadata["file_type"],
                        ChunkId = chunk.ChunkId,
                        ChunkCount = chunk.ChunkCount,
                        OriginalLength = doc.Text.Length,
                        ChunkLength = chunk.Content.Length,
                        ProcessingDate = DateTimeOffset.UtcNow,
                        ContentVector = embedding
                    };

                    searchDocuments.Add(searchDoc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Failed to generate embedding for chunk {chunk.ChunkId} of {metadata["file_name"]}: {ex.Message}");
                }
            }
        }

        Console.WriteLine($"\n✅ Prepared {searchDocuments.Count} policy document chunks for search index");

        if (searchDocuments.Count > 0)
        {
            var totalFiles = searchDocuments.Select(d => d.FileName).Distinct().Count();
            var totalChunks = searchDocuments.Count;
            var avgChunkLength = searchDocuments.Average(d => d.ChunkLength);

            Console.WriteLine($"\n📊 POLICIES INDEXING SUMMARY:");
            Console.WriteLine($"   📄 Total policy files: {totalFiles}");
            Console.WriteLine($"   🗂️ Total chunks created: {totalChunks}");
            Console.WriteLine($"   📏 Average chunk length: {avgChunkLength:F0} characters");

            var fileStats = searchDocuments.GroupBy(d => d.FileName)
                .Select(g => new { FileName = g.Key, Count = g.Count() });

            Console.WriteLine($"\n📋 Policy files breakdown:");
            foreach (var stat in fileStats)
            {
                Console.WriteLine($"   • {stat.FileName}: {stat.Count} chunks");
            }
        }

        return searchDocuments;
    }

    private async Task<float[]> GenerateEmbeddingAsync(object client, string text)
    {
        if (client is AzureOpenAIClient openAIClient)
        {
            var embeddingClient = openAIClient.GetEmbeddingClient(config.EmbeddingsModel);
            var result = await embeddingClient.GenerateEmbeddingAsync(text);
            return result.Value.ToFloats().ToArray();
        }
        else if (client is EmbeddingsClient inferenceClient)
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
}

public class ProcessedDocumentsWrapper
{
    [JsonPropertyName("policies")]
    public List<ProcessedDocument> Policies { get; set; } = [];
}

public class ProcessedDocument
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("metadata")]
    public DocumentMetadata? Metadata { get; set; }
}

public class DocumentMetadata
{
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("file_type")]
    public string FileType { get; set; } = string.Empty;
}

public class SearchDocument
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public int ChunkId { get; set; }
    public int ChunkCount { get; set; }
    public int OriginalLength { get; set; }
    public int ChunkLength { get; set; }
    public DateTimeOffset ProcessingDate { get; set; }
    public float[] ContentVector { get; set; } = Array.Empty<float>();
}
