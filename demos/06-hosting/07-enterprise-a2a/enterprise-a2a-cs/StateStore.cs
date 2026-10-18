using System.Text.Json.Nodes;

namespace EnterpriseA2A;

public class PreconditionFailedException(string message) : Exception(message);

public class SharedStateStore
{
    readonly Dictionary<(string, string), JsonObject> documents = new();
    readonly Dictionary<(string, string), JsonObject> cache = new();
    readonly object gate = new();
    readonly List<Action<JsonObject>> subscribers = new();
    int etagCounter;

    public int StoreReads { get; private set; }
    public IEnumerable<(string, string)> CacheKeys => cache.Keys;

    public void Subscribe(Action<JsonObject> callback) => subscribers.Add(callback);

    public void Create(string tenantId, string taskId)
    {
        lock (gate)
        {
            documents[(tenantId, taskId)] = new JsonObject
            {
                ["id"] = $"{tenantId}:{taskId}",
                ["tenantId"] = tenantId,
                ["taskId"] = taskId,
                ["contributions"] = new JsonObject(),
                ["_etag"] = (++etagCounter).ToString(),
            };
        }
    }

    public JsonObject Read(string tenantId, string taskId, bool useCache = true)
    {
        var key = (tenantId, taskId);
        lock (gate)
        {
            if (useCache && cache.TryGetValue(key, out JsonObject? cached))
                return (JsonObject)cached.DeepClone();
            JsonObject document = (JsonObject)documents[key].DeepClone();
            StoreReads++;
            cache[key] = (JsonObject)document.DeepClone();
            return document;
        }
    }

    public void Replace(string tenantId, string taskId, JsonObject document, string etag)
    {
        var key = (tenantId, taskId);
        lock (gate)
        {
            if ((string)documents[key]["_etag"]! != etag)
                throw new PreconditionFailedException($"etag mismatch for {tenantId}:{taskId}");
            document["_etag"] = (++etagCounter).ToString();
            documents[key] = (JsonObject)document.DeepClone();
            cache.Remove(key);
        }
        foreach (Action<JsonObject> callback in subscribers)
            callback(new JsonObject { ["tenantId"] = tenantId, ["taskId"] = taskId, ["reason"] = "state-update" });
    }

    public bool ReplaceIfUnmodified(string tenantId, string taskId, JsonObject document, string etag)
    {
        bool current;
        lock (gate) current = (string)documents[(tenantId, taskId)]["_etag"]! == etag;
        if (!current) return false;
        Replace(tenantId, taskId, document, etag);
        return true;
    }

    public async Task<int> ContributeAsync(string tenantId, string taskId, string agentId, JsonObject contribution, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            JsonObject document = Read(tenantId, taskId, useCache: false);
            JsonObject contributions = (JsonObject)document["contributions"]!;
            contributions[agentId] = new JsonObject
            {
                ["data"] = contribution.DeepClone(),
                ["version"] = contributions.Count + 1,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("o"),
            };
            try
            {
                Replace(tenantId, taskId, document, (string)document["_etag"]!);
                return attempt;
            }
            catch (PreconditionFailedException)
            {
                await Task.Delay(10);
            }
        }
        throw new PreconditionFailedException("retries exhausted");
    }

    public List<string> QueryTenantTasks(string tenantId)
    {
        lock (gate)
            return documents.Keys.Where(key => key.Item1 == tenantId).Select(key => key.Item2).ToList();
    }
}
