using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Storage.Blobs;

namespace RagAzure;

public class AzureServiceInitializer(AzureConfiguration config)
{

    public (BlobServiceClient, SearchIndexClient, SearchClient, object) InitializeClients()
    {
        Console.WriteLine("\n## 2. Initialize Azure Services");

        // Initialize Blob Service Client
        var blobServiceClient = new BlobServiceClient(config.StorageConnectionString);

        // Initialize Search Clients
        var searchCredential = new AzureKeyCredential(config.SearchAdminKey);
        var searchIndexClient = new SearchIndexClient(
            new Uri(config.SearchServiceEndpoint),
            searchCredential);
        var searchClient = new SearchClient(
            new Uri(config.SearchServiceEndpoint),
            config.SearchIndexName,
            searchCredential);

        // Initialize Embeddings Client
        object embeddingsClient;
        string authNote;

        if (IsOpenAIEndpoint(config.AzureAIModelsEndpoint))
        {
            AzureKeyCredential? credential = string.IsNullOrWhiteSpace(config.AzureAIModelsKey)
                ? null
                : new AzureKeyCredential(config.AzureAIModelsKey);

            if (credential != null)
            {
                embeddingsClient = new AzureOpenAIClient(new Uri(config.AzureAIModelsEndpoint), credential);
                authNote = "API key";
            }
            else
            {
                embeddingsClient = new AzureOpenAIClient(new Uri(config.AzureAIModelsEndpoint), new DefaultAzureCredential());
                authNote = "DefaultAzureCredential";
            }
        }
        else
        {
            AzureKeyCredential? credential = string.IsNullOrWhiteSpace(config.AzureAIModelsKey)
                ? null
                : new AzureKeyCredential(config.AzureAIModelsKey);

            if (credential != null)
            {
                embeddingsClient = new EmbeddingsClient(new Uri(config.AzureAIModelsEndpoint), credential);
                authNote = "API key";
            }
            else
            {
                embeddingsClient = new EmbeddingsClient(new Uri(config.AzureAIModelsEndpoint), new DefaultAzureCredential());
                authNote = "DefaultAzureCredential";
            }
        }

        // Verify connections
        var containers = blobServiceClient.GetBlobContainers().ToList();
        Console.WriteLine($"✅ Connected to Blob Storage - Found {containers.Count} containers");
        Console.WriteLine($"✅ Connected to Azure AI Search - Service is available");

        string endpointLabel = IsOpenAIEndpoint(config.AzureAIModelsEndpoint)
            ? "Azure OpenAI"
            : "Azure AI Inference";
        Console.WriteLine($"✅ Connected to {endpointLabel} - Endpoint: {config.AzureAIModelsEndpoint} ({authNote})");

        return (blobServiceClient, searchIndexClient, searchClient, embeddingsClient);
    }

    private static bool IsOpenAIEndpoint(string endpoint)
    {
        return endpoint.ToLower().Contains(".openai.azure.com");
    }
}
