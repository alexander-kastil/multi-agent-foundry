using System.Text.Json;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

static class SearchDemo
{
    const string IndexName = "m3-health";
    static readonly string[] Sources = ["drug_formulary", "clinical_guidelines", "lab_references"];

    public static async Task Run()
    {
        Uri searchEndpoint = new(Environment.GetEnvironmentVariable("SEARCH_ENDPOINT")!);
        string foundryEndpoint = Environment.GetEnvironmentVariable("FOUNDRY_ENDPOINT")!;
        DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions { ExcludeEnvironmentCredential = true, ExcludeManagedIdentityCredential = true });
        SearchIndexClient indexClient = new(searchEndpoint, credential);

        Console.WriteLine("== Load the documents into Azure AI Search");
        SearchIndex index = new(IndexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SimpleField("source", SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("title"),
                new SearchableField("content"),
                new VectorSearchField("content_vector", 1536, "vector-profile"),
            },
            VectorSearch = new VectorSearch
            {
                Algorithms = { new HnswAlgorithmConfiguration("hnsw") },
                Profiles = { new VectorSearchProfile("vector-profile", "hnsw") { VectorizerName = "openai" } },
                Vectorizers =
                {
                    new AzureOpenAIVectorizer("openai")
                    {
                        Parameters = new AzureOpenAIVectorizerParameters
                        {
                            ResourceUri = new Uri(foundryEndpoint),
                            DeploymentName = Models.Embedding,
                            ModelName = new AzureOpenAIModelName(Models.Embedding),
                        },
                    },
                },
            },
            SemanticSearch = new SemanticSearch
            {
                DefaultConfigurationName = "default",
                Configurations =
                {
                    new SemanticConfiguration("default", new SemanticPrioritizedFields
                    {
                        TitleField = new SemanticField("title"),
                        ContentFields = { new SemanticField("content") },
                    }),
                },
            },
        };
        await indexClient.CreateOrUpdateIndexAsync(index);

        List<(string Source, Doc Doc)> documents = Sources.SelectMany(source => LocalIndex.LoadSource(source).Select(doc => (source, doc))).ToList();
        float[][] vectors = await Models.Embed(documents.Select(pair => pair.Doc.Title + ". " + pair.Doc.Content).ToList());
        List<SearchDocument> uploads = documents.Select((pair, i) => new SearchDocument
        {
            ["id"] = pair.Doc.Id,
            ["source"] = pair.Source,
            ["title"] = pair.Doc.Title,
            ["content"] = pair.Doc.Content,
            ["content_vector"] = vectors[i],
        }).ToList();
        SearchClient search = new(searchEndpoint, IndexName, credential);
        await search.UploadDocumentsAsync(uploads);
        Console.WriteLine($"{IndexName}: {uploads.Count} documents");

        async Task<List<SearchResult<SearchDocument>>> Run(string query, string mode, string? source = null, int top = 10)
        {
            SearchOptions options = new() { Size = top, Select = { "id", "title" } };
            if (source is not null)
                options.Filter = $"source eq '{source}'";
            VectorizableTextQuery vector = new(query) { KNearestNeighborsCount = 50, Weight = 1.0f, Fields = { "content_vector" } };
            string? text = query;
            if (mode != "keyword")
                options.VectorSearch = new VectorSearchOptions { Queries = { vector } };
            if (mode == "vector")
                text = null;
            if (mode == "semantic")
            {
                options.QueryType = SearchQueryType.Semantic;
                options.SemanticSearch = new SemanticSearchOptions { SemanticConfigurationName = "default" };
            }
            SearchResults<SearchDocument> response = (await search.SearchAsync<SearchDocument>(text, options)).Value;
            List<SearchResult<SearchDocument>> results = [];
            await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
                results.Add(result);
            return results;
        }

        async Task<double> ReciprocalRank(string query, string expected, string mode, string source)
        {
            List<string> ids = (await Run(query, mode, source)).Select(result => (string)result.Document["id"]).ToList();
            int position = ids.IndexOf(expected);
            return position >= 0 ? 1.0 / (position + 1) : 0.0;
        }

        Console.WriteLine("\n== Mean reciprocal rank over the labeled queries (higher is better) ==");
        string[] modes = ["keyword", "vector", "hybrid", "hybrid + semantic"];
        Console.WriteLine($"{"source",-22}" + string.Concat(modes.Select(mode => $"{mode,-20}")));
        foreach (var (source, pairs) in Labeled.Pairs)
        {
            string row = "";
            foreach (string mode in new[] { "keyword", "vector", "hybrid", "semantic" })
            {
                double total = 0;
                foreach (var (query, expected) in pairs)
                    total += await ReciprocalRank(query, expected, mode, source);
                row += $"{total / pairs.Length,-20:0.00}";
            }
            Console.WriteLine($"{source,-22}{row}");
        }

        Console.WriteLine($"\n== Semantic ranker on: {Labeled.RerankQuery}");
        Console.WriteLine("hybrid order  : " + string.Join(" | ", (await Run(Labeled.RerankQuery, "hybrid", top: 5)).Select(result => (string)result.Document["title"])));
        List<SearchResult<SearchDocument>> reranked = await Run(Labeled.RerankQuery, "semantic", top: 5);
        Console.WriteLine("semantic order: " + string.Join(" | ", reranked.Select(result => $"{result.Document["title"]} ({result.SemanticSearch.RerankerScore:0.00})")));

        await indexClient.DeleteIndexAsync(IndexName, CancellationToken.None);
        Console.WriteLine($"\nDeleted index {IndexName}");
    }
}
