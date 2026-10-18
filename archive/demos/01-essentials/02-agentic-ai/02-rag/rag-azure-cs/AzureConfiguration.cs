namespace RagAzure;

public class AzureConfiguration
{
    public string ProjectEndpoint { get; set; } = string.Empty;
    public string EmbeddingsModel { get; set; } = "text-embedding-ada-002";
    public string AzureAIModelsEndpoint { get; set; } = string.Empty;
    public string? AzureAIModelsKey { get; set; }
    public string AssetsDir { get; set; } = "./assets/policies";
    public string StorageAccountName { get; set; } = string.Empty;
    public string StorageContainerName { get; set; } = "policy-uploads";
    public string StorageConnectionString { get; set; } = string.Empty;
    public string ProcessedBlobName { get; set; } = "processed_documents_for_vectorization.json";
    public string SearchServiceName { get; set; } = string.Empty;
    public string SearchServiceEndpoint { get; set; } = string.Empty;
    public string SearchAdminKey { get; set; } = string.Empty;
    public string SearchIndexName { get; set; } = "insurance-documents-index";
    public int ChunkSize { get; set; } = 1000;
    public int ChunkOverlap { get; set; } = 200;
}
