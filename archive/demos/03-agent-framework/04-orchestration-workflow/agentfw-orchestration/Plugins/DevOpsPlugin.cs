using Azure.AI.Agents.Persistent;
using System.Text.Json;

namespace AFWOrchestration;

public static class DevopsPlugin
{
    public static string OutcomeDirectory { get; set; } = string.Empty;

    private static void AppendToLogFile(string filepath, string content)
    {
        // Write to progress log file in outcome directory to track actions as they happen
        var fileName = Path.GetFileName(filepath);
        var progressLogPath = Path.Combine(OutcomeDirectory, fileName.Replace(".log", "-progress.log"));

        // Ensure outcome directory exists
        Directory.CreateDirectory(OutcomeDirectory);

        File.AppendAllText(progressLogPath, "\n" + content.Trim());
    }

    public static string RestartService(string serviceName = "", string logfile = "")
    {
        var logEntries = new[]
        {
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Multiple failures detected in {serviceName}. Restarting service.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO  {serviceName}: Restart initiated.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO  {serviceName}: Service restarted successfully."
        };

        var logMessage = string.Join("\n", logEntries);
        AppendToLogFile(logfile, logMessage);

        return $"Service {serviceName} restarted successfully.";
    }

    public static string RollbackTransaction(string logfile = "")
    {
        var logEntries = new[]
        {
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Transaction failure detected. Rolling back transaction batch.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   TransactionProcessor: Rolling back transaction batch.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   Transaction rollback completed successfully."
        };

        var logMessage = string.Join("\n", logEntries);
        AppendToLogFile(logfile, logMessage);

        return "Transaction rolled back successfully.";
    }

    public static string RedeployResource(string resourceName = "", string logfile = "")
    {
        var logEntries = new[]
        {
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Resource deployment failure detected in '{resourceName}'. Redeploying resource.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   DeploymentManager: Redeployment request submitted.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   DeploymentManager: Service successfully redeployed, resource '{resourceName}' created successfully."
        };

        var logMessage = string.Join("\n", logEntries);
        AppendToLogFile(logfile, logMessage);

        return $"Resource '{resourceName}' redeployed successfully.";
    }

    public static string IncreaseQuota(string logfile = "")
    {
        var logEntries = new[]
        {
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: High request volume detected. Increasing quota.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   APIManager: Quota increase request submitted.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO   APIManager: Quota successfully increased to 150% of previous limit."
        };

        var logMessage = string.Join("\n", logEntries);
        AppendToLogFile(logfile, logMessage);

        return "Successfully increased quota.";
    }

    public static string EscalateIssue(string logfile = "")
    {
        var logEntries = new[]
        {
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Cannot resolve issue.",
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ALERT  DevopsAssistant: Requesting escalation."
        };

        var logMessage = string.Join("\n", logEntries);
        AppendToLogFile(logfile, logMessage);

        return "Submitted escalation request.";
    }

    public static List<FunctionToolDefinition> GetToolDefinitions()
    {
        return new List<FunctionToolDefinition>
        {
            new FunctionToolDefinition(
                name: nameof(RestartService),
                description: "A function that restarts the named service",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        ServiceName = new { Type = "string", Description = "The name of the service to restart" },
                        Logfile = new { Type = "string", Description = "The path to the log file" }
                    },
                    Required = new[] { "serviceName", "logfile" }
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ),
            new FunctionToolDefinition(
                name: nameof(RollbackTransaction),
                description: "A function that rolls back the transaction",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        Logfile = new { Type = "string", Description = "The path to the log file" }
                    },
                    Required = new[] { "logfile" }
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ),
            new FunctionToolDefinition(
                name: nameof(RedeployResource),
                description: "A function that redeploys the named resource",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        ResourceName = new { Type = "string", Description = "The name of the resource to redeploy" },
                        Logfile = new { Type = "string", Description = "The path to the log file" }
                    },
                    Required = new[] { "resourceName", "logfile" }
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ),
            new FunctionToolDefinition(
                name: nameof(IncreaseQuota),
                description: "A function that increases the quota",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        Logfile = new { Type = "string", Description = "The path to the log file" }
                    },
                    Required = new[] { "logfile" }
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            ),
            new FunctionToolDefinition(
                name: nameof(EscalateIssue),
                description: "A function that escalates the issue",
                parameters: BinaryData.FromObjectAsJson(new
                {
                    Type = "object",
                    Properties = new
                    {
                        Logfile = new { Type = "string", Description = "The path to the log file" }
                    },
                    Required = new[] { "logfile" }
                },
                new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
            )
        };
    }
}
