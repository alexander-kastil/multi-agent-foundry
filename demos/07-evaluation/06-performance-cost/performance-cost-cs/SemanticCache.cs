using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI.Embeddings;

class HashStore
{
    readonly Dictionary<string, Dictionary<string, string>> hashes = new();
    readonly Dictionary<string, DateTimeOffset> expiry = new();

    public void Set(string key, Dictionary<string, string> fields, TimeSpan ttl)
    {
        hashes[key] = fields;
        expiry[key] = DateTimeOffset.UtcNow + ttl;
    }

    public Dictionary<string, string>? Get(string key)
    {
        if (expiry.TryGetValue(key, out DateTimeOffset until) && until < DateTimeOffset.UtcNow)
        {
            Delete(key);
            return null;
        }
        return hashes.GetValueOrDefault(key);
    }

    public List<string> Keys(string prefix) => hashes.Keys.Where(key => key.StartsWith(prefix)).ToList();

    public void Delete(string key)
    {
        hashes.Remove(key);
        expiry.Remove(key);
    }
}

class SemanticCache(HashStore store, EmbeddingClient? embeddings)
{
    const int LocalEmbeddingDimensions = 512;
    static readonly TimeSpan Week = TimeSpan.FromDays(7);
    readonly double threshold = double.Parse(Environment.GetEnvironmentVariable("CACHE_THRESHOLD") ?? "0.92", CultureInfo.InvariantCulture);

    public int Hits { get; private set; }
    public int Misses { get; private set; }

    async Task<double[]> GetEmbedding(string text)
    {
        if (embeddings is not null)
            return (await embeddings.GenerateEmbeddingAsync(text)).Value.ToFloats().ToArray().Select(value => (double)value).ToArray();
        double[] vector = new double[LocalEmbeddingDimensions];
        foreach (Match token in Regex.Matches(text.ToLowerInvariant(), "[a-z0-9]+"))
            vector[(int)(new System.Numerics.BigInteger(MD5.HashData(Encoding.UTF8.GetBytes(token.Value)), isUnsigned: true, isBigEndian: true) % LocalEmbeddingDimensions)] += 1;
        return vector;
    }

    static double Cosine(double[] first, double[] second)
    {
        double denominator = Math.Sqrt(first.Sum(x => x * x)) * Math.Sqrt(second.Sum(x => x * x));
        return denominator == 0 ? 0 : first.Zip(second, (a, b) => a * b).Sum() / denominator;
    }

    public async Task<(bool Hit, string? Response, double Similarity)> CheckCache(string query, string agentId)
    {
        double[] queryEmbedding = await GetEmbedding(query);
        Dictionary<string, string>? bestMatch = null;
        double bestSimilarity = 0;
        foreach (string key in store.Keys($"cache:{agentId}:").Take(500))
        {
            Dictionary<string, string>? cached = store.Get(key);
            if (cached is null) continue;
            double similarity = Cosine(queryEmbedding, cached["embedding"].Split(',').Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray());
            if (similarity > bestSimilarity)
            {
                bestSimilarity = similarity;
                bestMatch = cached;
            }
        }
        if (bestMatch is not null && bestSimilarity >= threshold)
        {
            Hits++;
            return (true, bestMatch["response"], bestSimilarity);
        }
        Misses++;
        return (false, null, bestSimilarity);
    }

    public async Task StoreCache(string query, string response, string agentId, string[] dependencies)
    {
        string key = $"cache:{agentId}:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(query)))[..16]}";
        store.Set(key, new()
        {
            ["embedding"] = string.Join(",", (await GetEmbedding(query)).Select(value => value.ToString("R", CultureInfo.InvariantCulture))),
            ["response"] = response,
            ["agent_id"] = agentId,
            ["dependencies"] = string.Join(",", dependencies),
        }, Week);
    }

    public int InvalidateByDependency(string dependencyId)
    {
        int removed = 0;
        foreach (string key in store.Keys("cache:").Concat(store.Keys("result:")))
        {
            if (store.Get(key)?["dependencies"].Split(',').Contains(dependencyId) == true)
            {
                store.Delete(key);
                removed++;
            }
        }
        return removed;
    }
}

class ResultCache(HashStore store)
{
    public int Hits { get; private set; }
    public int Misses { get; private set; }

    static string Key(string agentId, Dictionary<string, string> signature) =>
        $"result:{agentId}:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(signature.OrderBy(pair => pair.Key).ToDictionary()).ToLowerInvariant())))[..16]}";

    public string? Get(string agentId, Dictionary<string, string> signature)
    {
        Dictionary<string, string>? cached = store.Get(Key(agentId, signature));
        if (cached is not null)
        {
            Hits++;
            return cached["response"];
        }
        Misses++;
        return null;
    }

    public void Put(string agentId, Dictionary<string, string> signature, string response, string[] dependencies) =>
        store.Set(Key(agentId, signature), new() { ["response"] = response, ["dependencies"] = string.Join(",", dependencies) }, TimeSpan.FromDays(1));
}
