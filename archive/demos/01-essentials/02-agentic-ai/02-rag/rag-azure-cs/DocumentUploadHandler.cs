using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RagAzure;

public class DocumentUploadHandler(AzureConfiguration config)
{

    public async Task UploadDocumentsAsync(BlobServiceClient blobServiceClient)
    {
        Console.WriteLine("\n## 1. Upload documents from local folder to Azure Blob Storage");

        var containerClient = blobServiceClient.GetBlobContainerClient(config.StorageContainerName);

        // Create container if it doesn't exist
        await containerClient.CreateIfNotExistsAsync();

        var assetsDir = new DirectoryInfo(config.AssetsDir);
        if (!assetsDir.Exists)
        {
            Console.WriteLine($"❌ Assets directory not found: {config.AssetsDir}");
            return;
        }

        var files = assetsDir.GetFiles("*.md").OrderBy(f => f.Name).ToList();
        if (files.Count == 0)
        {
            Console.WriteLine($"❌ No markdown files found in {config.AssetsDir}");
            return;
        }

        // Create processed documents JSON structure
        var processedDocuments = new
        {
            policies = files.Select(file => new
            {
                text = File.ReadAllText(file.FullName),
                success = true,
                metadata = new
                {
                    file_name = file.Name,
                    file_type = "markdown"
                }
            }).ToList()
        };

        // Upload the processed documents JSON file
        var jsonData = JsonSerializer.Serialize(processedDocuments, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var blobClient = containerClient.GetBlobClient(config.ProcessedBlobName);
        var content = Encoding.UTF8.GetBytes(jsonData);

        await blobClient.UploadAsync(
            new BinaryData(content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/json"
                }
            });

        Console.WriteLine($"✅ Uploaded {processedDocuments.policies.Count} policy documents to {config.ProcessedBlobName}");
    }
}
