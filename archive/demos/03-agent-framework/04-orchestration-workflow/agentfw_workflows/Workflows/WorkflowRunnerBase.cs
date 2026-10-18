using AgentFwWorkflows.Models;

namespace AgentFwWorkflows.Workflows;

public abstract class WorkflowRunnerBase
{
    protected readonly AppConfig Config;
    protected readonly string DataPath;
    protected readonly string OutputPath;
    protected readonly string LogsPath;

    protected WorkflowRunnerBase(AppConfig config)
    {
        Config = config;
        DataPath = Path.Combine(Directory.GetCurrentDirectory(), config.DataPath);
        OutputPath = Path.Combine(Directory.GetCurrentDirectory(), config.OutputPath);
        LogsPath = Path.Combine(OutputPath, "logs");
    }

    public abstract Task RunAsync();

    protected void PrintHeader(string title, string description)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"🧾 {title}");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();
        Console.WriteLine(description);
        Console.WriteLine(new string('=', 80));
    }

    protected void PrintWorkflowComplete(string message, params string[] directories)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("🎉 WORKFLOW COMPLETE");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine(message);
        
        if (directories.Length > 0)
        {
            Console.WriteLine("\n📁 Check the following directories:");
            foreach (var dir in directories)
            {
                Console.WriteLine($"   • {dir}");
            }
        }
        
        Console.WriteLine(new string('=', 80));
    }
}
