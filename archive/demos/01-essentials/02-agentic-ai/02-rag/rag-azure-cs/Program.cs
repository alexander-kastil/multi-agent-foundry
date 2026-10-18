using Microsoft.Extensions.Configuration;
using RagAzure;
using Azure.Storage.Blobs;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        RAG Azure C# - Insurance Policy Search System          ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

// Load configuration
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var config = new AzureConfiguration();
configuration.GetSection("AzureConfig").Bind(config);

// Validate configuration
if (!ValidateConfiguration(config))
{
    Console.WriteLine("\n❌ Configuration validation failed. Please update appsettings.json");
    return;
}

// Initialize services (lazy initialization)
BlobServiceClient? blobServiceClient = null;
SearchIndexClient? searchIndexClient = null;
SearchClient? searchClient = null;
object? embeddingsClient = null;
SearchIndexManager? indexManager = null;
List<SearchDocument> processedDocuments = [];

bool running = true;

// Handle Ctrl+C gracefully
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true; // Prevent immediate termination
    running = false;
    Console.WriteLine("\n\n👋 Ctrl+C detected. Exiting gracefully...");
};

while (running)
{
    Console.WriteLine("\n" + new string('=', 64));
    Console.WriteLine("What would you like to do?");
    Console.WriteLine(new string('=', 64));
    Console.WriteLine("0. Run the whole process (executes all steps and exits)");
    Console.WriteLine("1. Upload documents from a local folder to Azure Blob Storage");
    Console.WriteLine("2. Initialize Azure Services");
    Console.WriteLine("3. Create Azure AI Search Index with Integrated Vectorization");
    Console.WriteLine("4. Retrieve and Process Documents");
    Console.WriteLine("5. Upload Documents to Search Index");
    Console.WriteLine("6. Test Search Index");
    Console.WriteLine("7. Run Complete Pipeline (steps 1-6)");
    Console.WriteLine("8. Exit");
    Console.Write("\nEnter your choice (0-8): ");

    var choice = Console.ReadLine();

    try
    {
        switch (choice)
        {
            case "0":
                await RunWholeProcess();
                running = false;
                break;
            case "1":
                await UploadDocuments();
                break;
            case "2":
                InitializeServices();
                break;
            case "3":
                await CreateSearchIndex();
                break;
            case "4":
                await ProcessDocuments();
                break;
            case "5":
                if (processedDocuments.Count == 0)
                {
                    Console.WriteLine("\n⚠️ No processed documents available. Please run step 4 first.");
                }
                else
                {
                    await UploadToSearchIndex();
                }
                break;
            case "6":
                await TestSearchIndex();
                break;
            case "7":
                await RunCompletePipeline();
                break;
            case "8":
                running = false;
                Console.WriteLine("\n👋 Goodbye!");
                break;
            default:
                Console.WriteLine("\n❌ Invalid choice. Please enter a number between 0 and 8.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ Error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

// Helper methods
async Task UploadDocuments()
{
    Console.WriteLine("\n📤 Starting document upload to Azure Blob Storage...");

    if (blobServiceClient == null)
    {
        InitializeServices();
    }

    var uploadHandler = new DocumentUploadHandler(config);
    await uploadHandler.UploadDocumentsAsync(blobServiceClient!);

    Console.WriteLine("✅ Document upload completed.");
}

void InitializeServices()
{
    Console.WriteLine("\n🔧 Initializing Azure services...");

    var initializer = new AzureServiceInitializer(config);
    (blobServiceClient, searchIndexClient, searchClient, embeddingsClient) = initializer.InitializeClients();
    indexManager = new SearchIndexManager(searchIndexClient, config.SearchIndexName);

    Console.WriteLine("✅ Azure services initialized.");
}

async Task CreateSearchIndex()
{
    Console.WriteLine("\n🔍 Creating Azure AI Search Index with Integrated Vectorization...");

    if (searchIndexClient == null)
    {
        InitializeServices();
    }

    await indexManager!.CreateSearchIndexAsync(config.EmbeddingsModel);

    Console.WriteLine("✅ Search index created.");
}

async Task ProcessDocuments()
{
    Console.WriteLine("\n📄 Retrieving and processing documents...");

    if (blobServiceClient == null || embeddingsClient == null)
    {
        InitializeServices();
    }

    var processor = new DocumentProcessor(config);
    processedDocuments = await processor.RetrieveAndProcessDocumentsAsync(
        blobServiceClient!,
        embeddingsClient!,
        config.StorageContainerName,
        config.ProcessedBlobName);

    Console.WriteLine("✅ Documents processed.");
}

async Task UploadToSearchIndex()
{
    Console.WriteLine("\n⬆️  Uploading documents to Search Index...");

    if (searchClient == null || indexManager == null)
    {
        InitializeServices();
    }

    var uploadHandler = new SearchUploadHandler(indexManager!);
    await uploadHandler.UploadDocumentsAsync(searchClient!, processedDocuments);

    Console.WriteLine("✅ Documents uploaded to search index.");
}

async Task TestSearchIndex()
{
    Console.WriteLine("\n🧪 Testing Search Index...");

    if (searchClient == null || embeddingsClient == null)
    {
        InitializeServices();
    }

    var tester = new SearchTester(searchClient!, embeddingsClient!, config);
    await tester.TestSearchIndexAsync();

    Console.WriteLine("✅ Search index test completed.");
}

async Task RunCompletePipeline()
{
    Console.WriteLine("\n🚀 Starting Complete RAG Pipeline");
    Console.WriteLine(new string('=', 64));

    await UploadDocuments();
    InitializeServices();
    await CreateSearchIndex();
    await ProcessDocuments();
    await UploadToSearchIndex();
    await TestSearchIndex();

    Console.WriteLine("\n✅ All steps completed successfully!");
}

async Task RunWholeProcess()
{
    Console.WriteLine("\n🚀 Running Complete Process (all steps and exit)");
    Console.WriteLine(new string('=', 64));

    await UploadDocuments();
    InitializeServices();
    await CreateSearchIndex();
    await ProcessDocuments();
    await UploadToSearchIndex();
    await TestSearchIndex();

    Console.WriteLine("\n✅ All steps completed successfully!");
    Console.WriteLine("👋 Exiting...");
}

static bool ValidateConfiguration(AzureConfiguration config)
{
    List<string> missingSettings = [];

    if (string.IsNullOrWhiteSpace(config.StorageConnectionString))
        missingSettings.Add("StorageConnectionString");
    if (string.IsNullOrWhiteSpace(config.SearchServiceEndpoint))
        missingSettings.Add("SearchServiceEndpoint");
    if (string.IsNullOrWhiteSpace(config.SearchAdminKey))
        missingSettings.Add("SearchAdminKey");
    if (string.IsNullOrWhiteSpace(config.AzureAIModelsEndpoint))
        missingSettings.Add("AzureAIModelsEndpoint");

    if (missingSettings.Count > 0)
    {
        Console.WriteLine("\n❌ Missing required configuration values:");
        foreach (var setting in missingSettings)
        {
            Console.WriteLine($"   - {setting}");
        }
        Console.WriteLine("➡️  Update your appsettings.json file and retry.");
        return false;
    }

    return true;
}