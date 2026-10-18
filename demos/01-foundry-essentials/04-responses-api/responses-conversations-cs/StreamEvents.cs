using System.Diagnostics;
using OpenAI.Responses;

#pragma warning disable OPENAI001

public static class StreamEvents
{
    public static async Task Run()
    {
        var started = Stopwatch.GetTimestamp();
        List<string> seen = [];
        double? firstToken = null;
        double total = 0;
        long outputTokens = 0;

        await foreach (StreamingResponseUpdate update in Session.Responses().CreateResponseStreamingAsync(
            "Explain in three sentences why streaming improves perceived latency."))
        {
            var type = update.GetType().Name.Replace("StreamingResponse", "").Replace("Update", "");
            if (!seen.Contains(type))
            {
                seen.Add(type);
            }

            if (update is StreamingResponseOutputTextDeltaUpdate delta)
            {
                firstToken ??= Stopwatch.GetElapsedTime(started).TotalSeconds;
                Console.Write(delta.Delta);
            }

            if (update is StreamingResponseCompletedUpdate completed)
            {
                total = Stopwatch.GetElapsedTime(started).TotalSeconds;
                outputTokens = completed.Response.Usage.OutputTokenCount;
            }
        }

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine($"event types : [{string.Join(", ", seen)}]");
        Console.WriteLine($"first token : {firstToken:F2}s");
        Console.WriteLine($"completed   : {total:F2}s ({outputTokens} output tokens)");
    }
}
