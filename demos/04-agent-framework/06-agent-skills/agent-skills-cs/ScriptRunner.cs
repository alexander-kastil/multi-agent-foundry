using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;

public static class ScriptRunner
{
    public static async Task<object?> RunAsync(
        AgentFileSkill skill,
        AgentFileSkillScript script,
        JsonElement? args,
        IServiceProvider? serviceProvider,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("python")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(script.FullPath);
        if (args is { ValueKind: JsonValueKind.Array } values)
        {
            foreach (var value in values.EnumerateArray())
            {
                startInfo.ArgumentList.Add(value.GetString()!);
            }
        }

        using var process = Process.Start(startInfo)!;
        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output.Trim();
    }
}
