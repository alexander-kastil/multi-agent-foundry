using System.Text.RegularExpressions;

namespace RagAzure;

public class TextChunker(int chunkSize = 1000, int chunkOverlap = 200)
{

    public string CleanText(string text)
    {
        text = Regex.Replace(text, @"\s+", " ");
        text = Regex.Replace(text, @"\n+", "\n");
        return text.Trim();
    }

    public List<ChunkResult> ChunkTextForSearch(string text, Dictionary<string, string> metadata)
    {
        text = CleanText(text);
        var chunks = new List<ChunkResult>();

        if (text.Length <= chunkSize)
        {
            return
            [
                new()
                {
                    Content = text,
                    ChunkId = 0,
                    ChunkCount = 1,
                    Metadata = new(metadata)
                }
            ];
        }

        int start = 0;
        int chunkId = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);

            if (end < text.Length)
            {
                int sentenceEnd = text.LastIndexOf('.', end - 1, end - start);
                if (sentenceEnd > start)
                {
                    end = sentenceEnd + 1;
                }
            }

            string chunkText = text.Substring(start, end - start).Trim();

            if (!string.IsNullOrWhiteSpace(chunkText))
            {
                chunks.Add(new()
                {
                    Content = chunkText,
                    ChunkId = chunkId,
                    ChunkCount = 0, // Will be updated after all chunks are created
                    Metadata = new(metadata)
                });
                chunkId++;
            }

            start = Math.Max(start + chunkSize - chunkOverlap, end);
        }

        // Update chunk count for all chunks
        foreach (var chunk in chunks)
        {
            chunk.ChunkCount = chunks.Count;
        }

        return chunks;
    }
}

public class ChunkResult
{
    public string Content { get; set; } = string.Empty;
    public int ChunkId { get; set; }
    public int ChunkCount { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
