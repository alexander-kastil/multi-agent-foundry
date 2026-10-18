using Azure.Search.Documents;

namespace RagAzure;

public class SearchUploadHandler(SearchIndexManager indexManager)
{

    public async Task<bool> UploadDocumentsAsync(SearchClient searchClient, List<SearchDocument> searchDocuments)
    {
        Console.WriteLine("\n## 6. Upload Documents to Azure AI Search with Pre-computed Embeddings");

        if (searchDocuments.Count == 0)
        {
            Console.WriteLine("❌ No documents to upload");
            return false;
        }

        Console.WriteLine($"📤 Uploading {searchDocuments.Count} documents to search index...");

        try
        {
            const int batchSize = 100;
            int totalBatches = (int)Math.Ceiling(searchDocuments.Count / (double)batchSize);

            for (int i = 0; i < totalBatches; i++)
            {
                var batch = searchDocuments.Skip(i * batchSize).Take(batchSize).ToList();
                var azureDocuments = batch.Select(doc => new Azure.Search.Documents.Models.SearchDocument
                {
                    ["id"] = doc.Id,
                    ["title"] = doc.Title,
                    ["content"] = doc.Content,
                    ["category"] = doc.Category,
                    ["file_name"] = doc.FileName,
                    ["file_type"] = doc.FileType,
                    ["chunk_id"] = doc.ChunkId,
                    ["chunk_count"] = doc.ChunkCount,
                    ["original_length"] = doc.OriginalLength,
                    ["chunk_length"] = doc.ChunkLength,
                    ["processing_date"] = doc.ProcessingDate,
                    ["content_vector"] = doc.ContentVector
                }).ToList();

                var result = await searchClient.UploadDocumentsAsync(azureDocuments);
                Console.WriteLine($"📊 Uploaded batch {i + 1}/{totalBatches} ({batch.Count} documents)");
            }

            Console.WriteLine($"✅ Successfully uploaded {searchDocuments.Count} documents to search index");

            // Get and display index stats
            var stats = await indexManager.GetIndexStatsAsync();
            if (stats.Count > 0)
            {
                Console.WriteLine($"\n📊 Index Statistics:");
                Console.WriteLine($"   📝 Index name: {stats["name"]}");
                Console.WriteLine($"   📋 Field count: {stats["field_count"]}");
                Console.WriteLine($"   📄 Document count: {stats["document_count"]}");
                Console.WriteLine($"   💾 Storage size: {stats["storage_size"]} bytes");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to upload documents: {ex.Message}");
            return false;
        }
    }
}
