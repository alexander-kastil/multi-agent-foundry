using System.Text.Json;

namespace AFWOrchestration;

public static class ToolCallHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string HandleToolCall(string functionName, string argumentsJson)
    {
        try
        {
            switch (functionName)
            {
                case nameof(LogFilePlugin.ReadLogFile):
                    var readArgs = JsonSerializer.Deserialize<ReadLogFileArgs>(argumentsJson, JsonOptions);
                    return LogFilePlugin.ReadLogFile(readArgs?.Filepath ?? "");

                case nameof(DevopsPlugin.RestartService):
                    var restartArgs = JsonSerializer.Deserialize<ServiceArgs>(argumentsJson, JsonOptions);
                    return DevopsPlugin.RestartService(restartArgs?.ServiceName ?? "", restartArgs?.Logfile ?? "");

                case nameof(DevopsPlugin.RollbackTransaction):
                    var rollbackArgs = JsonSerializer.Deserialize<LogfileArgs>(argumentsJson, JsonOptions);
                    return DevopsPlugin.RollbackTransaction(rollbackArgs?.Logfile ?? "");

                case nameof(DevopsPlugin.RedeployResource):
                    var redeployArgs = JsonSerializer.Deserialize<ResourceArgs>(argumentsJson, JsonOptions);
                    return DevopsPlugin.RedeployResource(redeployArgs?.ResourceName ?? "", redeployArgs?.Logfile ?? "");

                case nameof(DevopsPlugin.IncreaseQuota):
                    var quotaArgs = JsonSerializer.Deserialize<LogfileArgs>(argumentsJson, JsonOptions);
                    return DevopsPlugin.IncreaseQuota(quotaArgs?.Logfile ?? "");

                case nameof(DevopsPlugin.EscalateIssue):
                    var escalateArgs = JsonSerializer.Deserialize<LogfileArgs>(argumentsJson, JsonOptions);
                    return DevopsPlugin.EscalateIssue(escalateArgs?.Logfile ?? "");

                default:
                    return $"Unknown function: {functionName}";
            }
        }
        catch (Exception ex)
        {
            return $"Error executing {functionName}: {ex.Message}";
        }
    }

    private record ReadLogFileArgs(string Filepath);
    private record ServiceArgs(string ServiceName, string Logfile);
    private record ResourceArgs(string ResourceName, string Logfile);
    private record LogfileArgs(string Logfile);
}
