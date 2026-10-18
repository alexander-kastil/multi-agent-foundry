using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace AFWOrchestration;

public static class LogFilePlugin
{
    public static string OutcomeDirectory { get; set; } = string.Empty;

    public static void PrintLogSummary(string filepath)
    {
        try
        {
            var lines = File.ReadAllLines(filepath);
            int errorCount = lines.Count(l => l.Contains(" ERROR "));
            int warningCount = lines.Count(l => l.Contains(" WARNING "));
            int alertCount = lines.Count(l => l.Contains(" ALERT "));
            int criticalCount = lines.Count(l => l.Contains(" CRITICAL "));
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Summary: errors={errorCount}, warnings={warningCount}, alerts={alertCount}, critical={criticalCount}");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Summary unavailable: {ex.Message}");
        }
        finally
        {
            Console.ResetColor();
        }
    }

    public static void WriteOutcome(string originalLogPath, string outcomeText)
    {
        var fileName = Path.GetFileName(originalLogPath);
        Directory.CreateDirectory(OutcomeDirectory);
        var outcomeLogPath = Path.Combine(OutcomeDirectory, fileName.Replace(".log", "-outcome.log"));
        File.WriteAllText(outcomeLogPath, outcomeText);
    }

    public static void PrintOutcome(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    public static string ReadLogFile(string filepath = "")
    {
        var originalLog = File.ReadAllText(filepath);

        // Look for progress log in outcome directory
        var fileName = Path.GetFileName(filepath);
        var progressLogPath = Path.Combine(OutcomeDirectory, fileName.Replace(".log", "-progress.log"));

        // Check if progress log exists and append it
        if (File.Exists(progressLogPath))
        {
            var progressLog = File.ReadAllText(progressLogPath);
            return $"{originalLog}\n\n--- ACTIONS IN PROGRESS ---\n{progressLog}";
        }

        return originalLog;
    }

    public static FunctionToolDefinition GetToolDefinition()
    {
        return new FunctionToolDefinition(
            name: nameof(ReadLogFile),
            description: "Accesses the given file path string and returns the file contents as a string",
            parameters: BinaryData.FromObjectAsJson(new
            {
                Type = "object",
                Properties = new
                {
                    Filepath = new
                    {
                        Type = "string",
                        Description = "The path to the log file to read"
                    }
                },
                Required = new[] { "filepath" }
            },
            new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        );
    }
}
