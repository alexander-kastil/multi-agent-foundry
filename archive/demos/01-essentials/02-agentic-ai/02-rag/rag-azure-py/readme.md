# Build a custom chat app with the Azure AI Foundry SDK

[Build a custom chat app with the Azure AI Foundry SDK](https://learn.microsoft.com/en-us/azure/ai-studio/tutorials/copilot-sdk-create-resources?tabs=windows)

## Setup

1. Install dependencies:

   ```bash
   uv sync
   ```

2. Copy the template and configure your environment:

   ```bash
   cp .env.copy .env
   ```

3. Edit `.env` and set your `PROJECT_ENDPOINT`, `AISEARCH_INDEX_NAME`, `INTENT_MAPPING_MODEL`, `EMBEDDINGS_MODEL`, `EVALUATION_MODEL`, and Azure Search credentials:

   ```
   PROJECT_ENDPOINT=https://your-ai-services-account-name.services.ai.azure.com/api/projects/your-project-name
   AISEARCH_INDEX_NAME=product-index
   INTENT_MAPPING_MODEL=gpt-4.1-mini
   EMBEDDINGS_MODEL=text-embedding-3-small
   EVALUATION_MODEL=gpt-4.1-mini
   STORAGE_CONNECTION_STRING=your_storage_connection_string
   SEARCH_SERVICE_NAME=your_search_service_name
   SEARCH_SERVICE_ENDPOINT=https://your-search-service.search.windows.net
   SEARCH_ADMIN_KEY=your_search_admin_key
   ```

4. Create the vectorized index (for insurance documents):

   ```bash
   uv run python create_vectorized_index.py
   ```

   This script creates an Azure AI Search index with pre-computed embeddings for insurance policy documents.

## Insurance Policy Vector RAG Demo Overview

This demo ingests a set of markdown insurance policy documents, chunks them, generates embeddings using an Azure OpenAI (or Azure AI Inference) deployment, and loads those chunks into an Azure AI Search index with vector + semantic configuration. After indexing, sample queries demonstrate hybrid retrieval.

### High-Level Flow

1. Upload raw policy files to Blob Storage as a single processed JSON (`upload-policies.py`).
2. Create (or recreate) a vector-enabled search index (`create_vectorized_index.py`).
3. Retrieve processed documents, chunk text, generate embeddings, and upload documents.
4. Run sample queries to validate retrieval quality.

### Module Reference

| File                             | Purpose                                                                                                              |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `upload-policies.py`             | Reads local policy markdown files and uploads a consolidated JSON blob of processed documents to Azure Blob Storage. |
| `create_vectorized_index.py`     | Orchestrates the full pipeline: init clients, create index, process docs, embed, upload, test.                       |
| `initialize_clients.py`          | Initializes Blob, Search, and Embeddings clients; auto-detects Azure OpenAI vs. generic inference endpoint.          |
| `search_index_manager.py`        | Creates and manages the Azure AI Search index (vector + semantic configuration).                                     |
| `document_retriever.py`          | Downloads and parses the processed documents JSON from Blob Storage.                                                 |
| `text_chunker.py`                | Splits large document text into overlapping chunks for better retrieval granularity.                                 |
| `document_processor.py`          | Orchestrates chunking and embedding generation; builds search document payloads.                                     |
| `search_index_uploader.py`       | Handles batched upload of documents into the search index.                                                           |
| `upload_handler.py`              | Wraps upload process and prints index stats post-ingestion.                                                          |
| `search_tester.py`               | Executes vector/semantic style queries; includes fallback if semantic search feature isn't enabled.                  |
| `test_search.py`                 | Provides sample insurance-related queries for quick validation.                                                      |
| `2.document-vectorization.ipynb` | Original notebook prototype for the pipeline.                                                                        |

### Quick Start (Policies RAG)

```bash
uv sync
cp .env.copy .env
# Edit .env with real endpoints, keys, and resource names

# 1. Upload processed policy JSON to Blob
uv run python upload-policies.py

# 2. Build index and ingest + test
uv run python create_vectorized_index.py
```

### Environment Variables (Key Ones)

| Variable                       | Description                                                                |
| ------------------------------ | -------------------------------------------------------------------------- |
| `PROJECT_ENDPOINT`             | Azure AI Project endpoint (used for broader project integrations).         |
| `AZURE_AI_MODELS_ENDPOINT`     | Endpoint for embeddings deployment (Azure OpenAI or inference).            |
| `EMBEDDINGS_MODEL`             | Deployment name for embedding generation (e.g., `text-embedding-ada-002`). |
| `STORAGE_CONNECTION_STRING`    | Connection string for Blob Storage.                                        |
| `STORAGE_CONTAINER_NAME`       | Container hosting the processed documents blob.                            |
| `PROCESSED_BLOB_NAME`          | Name of the JSON blob with processed policy docs.                          |
| `SEARCH_SERVICE_ENDPOINT`      | Azure AI Search service endpoint.                                          |
| `SEARCH_ADMIN_KEY`             | Admin API key for index management & document upload.                      |
| `SEARCH_INDEX_NAME`            | Name of the index to create (e.g., `insurance-documents-index`).           |
| `CHUNK_SIZE` / `CHUNK_OVERLAP` | Chunking parameters for document splitting.                                |

### Notes

- Re-running `create_vectorized_index.py` repeatedly without deleting the index will accumulate duplicate chunks; delete or change the index name for a clean run.
- If semantic search isn't enabled on your SKU, the tester automatically falls back to simple query mode.
- Leave `AZURE_AI_MODELS_KEY` blank to use `DefaultAzureCredential` with managed identity / developer login.

5. Create the search index:

   ```bash
   uv run python create_search_index.py
   ```

6. Load product documents:
   ```bash
   uv run python get_product_documents.py
   ```

## Test with prompt:

```bash
uv run python chat_with_products.py --query "I need a new tent for 4 people, what would you recommend?"
```

## Test with logging:

```bash
uv run python chat_with_products.py --query "I need a new tent for 4 people, what would you recommend?" --enable-telemetry
```

## Run evaluations:

```bash
uv run python evaluate.py
```

## Document Vectorization

The `create_vectorized_index.py` script implements the document vectorization workflow from the `2.document-vectorization.ipynb` notebook. The implementation is organized into separate modules for better readability:

**Modules:**

- `initialize_clients.py`: Azure service client initialization
- `search_index_manager.py`: Search index creation and management
- `document_retriever.py`: Document retrieval from blob storage
- `text_chunker.py`: Text chunking utilities
- `document_processor.py`: Document processing and embedding generation
- `search_index_uploader.py`: Document upload to search index
- `search_tester.py`: Search testing utilities
- `test_search.py`: Sample query testing
- `upload_handler.py`: Upload orchestration

**Workflow Steps:**

1. **Initialize Azure Services**: Connect to Azure Blob Storage, Azure AI Search, and Azure AI Inference
2. **Create Search Index**: Set up an Azure AI Search index with vector search and semantic search capabilities
3. **Retrieve Documents**: Download processed insurance documents from Azure Blob Storage
4. **Process Documents**: Chunk documents into optimally-sized pieces with overlapping content
5. **Generate Embeddings**: Create vector embeddings using Azure AI Inference EmbeddingsClient
6. **Upload Documents**: Index the documents with their embeddings in Azure AI Search
7. **Test Search**: Validate the index with sample insurance-related queries

The script processes only policy documents from the `processed-documents` container and creates a searchable index with both semantic and vector search capabilities.
