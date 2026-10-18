# RAG Azure C# - Insurance Policy Search System

This is a C# implementation of a Retrieval-Augmented Generation (RAG) system using Azure AI services. It demonstrates how to build a document search system with vector embeddings and semantic search capabilities.

## Overview

This application processes insurance policy documents, generates embeddings, and enables semantic search using:
- **Azure Blob Storage**: For document storage
- **Azure AI Search**: For vector and semantic search
- **Azure OpenAI / Azure AI Inference**: For generating text embeddings

## Prerequisites

- .NET 9.0 or later
- Azure subscription with the following resources:
  - Azure Storage Account
  - Azure AI Search service
  - Azure OpenAI or Azure AI Inference endpoint

## Setup

1. **Update Configuration**

   Edit `appsettings.json` and configure your Azure resources:

   ```json
   {
     "AzureConfig": {
       "ProjectEndpoint": "https://your-ai-project.services.ai.azure.com/api/projects/your-project-name",
       "EmbeddingsModel": "text-embedding-ada-002",
       "AzureAIModelsEndpoint": "https://your-openai-resource.openai.azure.com/",
       "AzureAIModelsKey": "",
       "StorageConnectionString": "your_storage_connection_string",
       "StorageContainerName": "policy-uploads",
       "SearchServiceEndpoint": "https://your-search-service.search.windows.net",
       "SearchAdminKey": "your_search_admin_key",
       "SearchIndexName": "insurance-documents-index"
     }
   }
   ```

2. **Restore Dependencies**

   ```bash
   dotnet restore
   ```

3. **Build the Project**

   ```bash
   dotnet build
   ```

## Usage

Run the application:

```bash
dotnet run
```

### Interactive Menu

The application provides an interactive menu with the following options:

1. **Upload documents from a local folder to Azure Blob Storage**
   - Uploads markdown files from the `assets/policies` folder to Azure Blob Storage
   - Creates a processed JSON file with document metadata

2. **Initialize Azure Services**
   - Connects to Azure Blob Storage, Azure AI Search, and Azure OpenAI/AI Inference
   - Verifies all connections are successful

3. **Create Azure AI Search Index with Integrated Vectorization**
   - Creates a search index with vector and semantic search capabilities
   - Configures fields for document metadata and content vectors

4. **Retrieve and Process Documents**
   - Downloads processed documents from Blob Storage
   - Chunks documents into smaller pieces (default: 1000 characters with 200 character overlap)
   - Generates embeddings for each chunk using Azure OpenAI/AI Inference

5. **Upload Documents to Search Index**
   - Uploads document chunks with their embeddings to Azure AI Search
   - Displays upload progress and index statistics

6. **Test Search Index**
   - Runs sample insurance-related queries against the search index
   - Demonstrates vector search with semantic ranking
   - Displays top results for each query

7. **Run Complete Pipeline (steps 1-6)**
   - Executes all steps in sequence for a complete end-to-end workflow

8. **Exit**

## Architecture

### Components

- **AzureConfiguration.cs**: Strongly-typed configuration class
- **AzureServiceInitializer.cs**: Initializes Azure service clients
- **DocumentUploadHandler.cs**: Handles document upload to Blob Storage
- **SearchIndexManager.cs**: Manages Azure AI Search index creation and configuration
- **TextChunker.cs**: Splits documents into chunks for better search granularity
- **DocumentProcessor.cs**: Processes documents and generates embeddings
- **SearchUploadHandler.cs**: Uploads documents to the search index
- **SearchTester.cs**: Tests the search index with sample queries

### Workflow

1. **Upload**: Raw policy documents are uploaded to Azure Blob Storage as a consolidated JSON
2. **Index Creation**: A search index with vector and semantic search capabilities is created
3. **Processing**: Documents are chunked and embeddings are generated
4. **Indexing**: Document chunks with embeddings are uploaded to Azure AI Search
5. **Search**: Vector search with semantic ranking retrieves relevant documents

## Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `EmbeddingsModel` | Model deployment name for embeddings | `text-embedding-ada-002` |
| `AssetsDir` | Local directory containing policy documents | `./assets/policies` |
| `StorageContainerName` | Blob container name | `policy-uploads` |
| `ProcessedBlobName` | Name of the processed documents JSON blob | `processed_documents_for_vectorization.json` |
| `SearchIndexName` | Name of the search index | `insurance-documents-index` |
| `ChunkSize` | Maximum size of text chunks in characters | `1000` |
| `ChunkOverlap` | Overlap between consecutive chunks | `200` |

## Sample Queries

The application includes these sample insurance queries:
- "What is covered under collision insurance?"
- "How much does comprehensive coverage cost?"
- "What are the liability limits for commercial vehicles?"
- "Does my policy cover theft and vandalism?"
- "What happens if I hit an uninsured driver?"
- "High value vehicle insurance requirements"
- "Motorcycle insurance coverage options"

## Notes

- **Authentication**: Leave `AzureAIModelsKey` blank to use `DefaultAzureCredential` with managed identity or developer login
- **Semantic Search**: If semantic search isn't enabled on your Azure AI Search SKU, the application will fall back to vector-only search
- **Re-running**: Running the complete pipeline multiple times without deleting the index will accumulate duplicate chunks

## Reference

Based on Microsoft Learn tutorial:
- [Get started with RAG using Azure AI Search](https://learn.microsoft.com/en-us/azure/search/search-get-started-rag?pivots=csharp)

## Python Version

This C# implementation is based on the Python version located in `../rag-azure-py/`.
