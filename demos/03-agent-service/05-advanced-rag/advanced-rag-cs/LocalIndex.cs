using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using System.ClientModel.Primitives;
using OpenAI;
using OpenAI.Embeddings;

record Doc(string Id, string Title, string Content);

record Hit(string Id, string Title, string Content, double Score, string Source);

class Models
{
    public static readonly string Chat = Environment.GetEnvironmentVariable("MODEL_DEPLOYMENT_NAME")!;
    public static readonly string Embedding = Environment.GetEnvironmentVariable("EMBEDDING_MODEL_DEPLOYMENT_NAME")!;
    public static readonly AIProjectClient Project = new(new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!), new DefaultAzureCredential());
    public static readonly EmbeddingClient Embeddings = new OpenAIClient(
        new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
        new OpenAIClientOptions { Endpoint = new Uri(Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")![..Environment.GetEnvironmentVariable("PROJECT_ENDPOINT")!.IndexOf("/api/projects")] + "/openai/v1/") })
        .GetEmbeddingClient(Embedding);
    public static readonly ProjectResponsesClient Responses = Project.ProjectOpenAIClient.GetProjectResponsesClientForModel(Chat);

    const string CachePath = ".cache/embeddings.json";

    public static async Task<float[][]> Embed(IList<string> texts)
    {
        Dictionary<string, float[]> cache = File.Exists(CachePath)
            ? JsonSerializer.Deserialize<Dictionary<string, float[]>>(File.ReadAllText(CachePath))!
            : [];
        string[] keys = texts.Select(text => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{Embedding}:{text}")))).ToArray();
        List<(string Key, string Text)> missing = keys.Zip(texts).Where(pair => !cache.ContainsKey(pair.First)).ToList();
        if (missing.Count > 0)
        {
            OpenAIEmbeddingCollection response = (await Embeddings.GenerateEmbeddingsAsync(missing.Select(pair => pair.Text).ToList())).Value;
            for (int i = 0; i < missing.Count; i++)
                cache[missing[i].Key] = response[i].ToFloats().ToArray();
            Directory.CreateDirectory(".cache");
            File.WriteAllText(CachePath, JsonSerializer.Serialize(cache));
        }
        return keys.Select(key => cache[key]).ToArray();
    }

    public static float[] Normalize(float[] vector)
    {
        double norm = Math.Sqrt(vector.Sum(value => (double)value * value));
        return vector.Select(value => (float)(value / norm)).ToArray();
    }

    public static double Dot(float[] a, float[] b) => a.Zip(b, (x, y) => (double)x * y).Sum();
}

class LocalIndex
{
    const int RrfK = 60;
    static readonly HashSet<string> Stopwords = ["a", "an", "and", "in", "of", "on", "or", "the", "to", "with", "for", "is", "are", "after", "what", "should", "i", "ndc"];

    readonly string name;
    readonly List<Doc> docs;
    readonly List<List<string>> tokens;
    readonly double avgLen;
    readonly Dictionary<string, int> docFreq = [];
    float[][] vectors = [];

    LocalIndex(List<Doc> docs, string name)
    {
        this.docs = docs;
        this.name = name;
        tokens = docs.Select(doc => Tokenize(doc.Title + " " + doc.Content)).ToList();
        avgLen = tokens.Average(list => list.Count);
        foreach (List<string> list in tokens)
            foreach (string term in list.Distinct())
                docFreq[term] = docFreq.GetValueOrDefault(term) + 1;
    }

    public static List<Doc> LoadSource(string name) =>
        JsonSerializer.Deserialize<List<Doc>>(File.ReadAllText($"data/{name}.json"), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    public static async Task<LocalIndex> Create(string name)
    {
        LocalIndex index = new(LoadSource(name), name);
        float[][] raw = await Models.Embed(index.docs.Select(doc => doc.Title + ". " + doc.Content).ToList());
        index.vectors = raw.Select(Models.Normalize).ToArray();
        return index;
    }

    static List<string> Tokenize(string text) =>
        Regex.Matches(text.ToLowerInvariant(), @"[a-z0-9]+(?:[-.][a-z0-9]+)*").Select(match => match.Value).Where(term => !Stopwords.Contains(term)).ToList();

    double[] Bm25Scores(string query, double k1 = 1.5, double b = 0.75)
    {
        int n = docs.Count;
        double[] scores = new double[n];
        foreach (string term in Tokenize(query))
        {
            if (!docFreq.TryGetValue(term, out int df))
                continue;
            double idf = Math.Log(1 + (n - df + 0.5) / (df + 0.5));
            for (int i = 0; i < n; i++)
            {
                int tf = tokens[i].Count(token => token == term);
                if (tf > 0)
                    scores[i] += idf * tf * (k1 + 1) / (tf + k1 * (1 - b + b * tokens[i].Count / avgLen));
            }
        }
        return scores;
    }

    async Task<double[]> VectorScores(string query)
    {
        float[] q = Models.Normalize((await Models.Embed([query]))[0]);
        return vectors.Select(vector => Models.Dot(vector, q)).ToArray();
    }

    static IEnumerable<int> Rank(double[] scores) => Enumerable.Range(0, scores.Length).OrderByDescending(i => scores[i]);

    public async Task<List<Hit>> Search(string query, string mode = "hybrid", double vectorWeight = 1.0, int top = 3)
    {
        double[] bm25 = Bm25Scores(query);
        double[] vec = await VectorScores(query);
        Dictionary<int, double> scores = [];
        List<int> order;
        if (mode == "keyword")
        {
            order = Rank(bm25).Where(i => bm25[i] > 0).ToList();
            foreach (int i in order) scores[i] = bm25[i];
        }
        else if (mode == "vector")
        {
            order = Rank(vec).ToList();
            foreach (int i in order) scores[i] = vec[i];
        }
        else
        {
            int rank = 0;
            foreach (int i in Rank(bm25).Where(i => bm25[i] > 0))
                scores[i] = scores.GetValueOrDefault(i) + 1.0 / (RrfK + ++rank);
            rank = 0;
            foreach (int i in Rank(vec))
                scores[i] = scores.GetValueOrDefault(i) + vectorWeight / (RrfK + ++rank);
            order = scores.Keys.OrderByDescending(i => scores[i]).ToList();
        }
        return order.Take(top).Select(i => new Hit(docs[i].Id, docs[i].Title, docs[i].Content, scores[i], name)).ToList();
    }
}
