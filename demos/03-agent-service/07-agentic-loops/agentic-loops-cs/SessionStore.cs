using System.Text.Json;

record Session(string ConversationId, string LastResponseId, Dictionary<string, string> Metadata, DateTimeOffset UpdatedAt);

static class SessionStore
{
    static readonly string Path = System.IO.Path.Combine(AppContext.BaseDirectory, "sessions.json");

    static Dictionary<string, Session> Load() =>
        File.Exists(Path) ? JsonSerializer.Deserialize<Dictionary<string, Session>>(File.ReadAllText(Path))! : [];

    public static void Save(string sessionId, string conversationId, string lastResponseId, Dictionary<string, string> metadata)
    {
        Dictionary<string, Session> sessions = Load();
        sessions[sessionId] = new Session(conversationId, lastResponseId, metadata, DateTimeOffset.UtcNow);
        File.WriteAllText(Path, JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static Session Get(string sessionId) => Load()[sessionId];
}
