using Microsoft.Extensions.Configuration;
using AgentFwWorkflows.Models;
using AgentFwWorkflows.Workflows;

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

IConfiguration configuration = builder.Build();
var appConfig = AppConfig.FromConfiguration(configuration);

while (true)
{
    Console.Clear();
    Console.WriteLine("=== Azure Agent Framework Workflows - Demo Menu ===\n");
    Console.WriteLine("1.  Sequential Workflow - Linear 5-step workflow");
    Console.WriteLine("2.  Concurrent Workflow - Parallel processing with fan-out/fan-in");
    Console.WriteLine("3.  Branching Workflow - Conditional routing with switch-case");
    Console.WriteLine("4.  Visualization Workflow - Pattern visualizer");
    Console.WriteLine("5.  Interactive Checkpointing - Human-in-the-loop with state persistence");
    Console.WriteLine("6.  Agents in Workflow - AI agents integrated into workflow steps");
    Console.WriteLine("\nPress Ctrl+C to exit");
    Console.Write("\nSelect a demo (1-6): ");

    string? choice = Console.ReadLine();
    Console.Clear();

    try
    {
        switch (choice)
        {
            case "1":
                Console.WriteLine("=== Demo 1: Sequential Workflow ===\n");
                var sequentialWorkflow = new SequentialWorkflow(appConfig);
                await sequentialWorkflow.RunAsync();
                break;

            case "2":
                Console.WriteLine("=== Demo 2: Concurrent Workflow ===\n");
                var concurrentWorkflow = new ConcurrentWorkflow(appConfig);
                await concurrentWorkflow.RunAsync();
                break;

            case "3":
                Console.WriteLine("=== Demo 3: Branching Workflow ===\n");
                var branchingWorkflow = new BranchingWorkflow(appConfig);
                await branchingWorkflow.RunAsync();
                break;

            case "4":
                Console.WriteLine("=== Demo 4: Visualization Workflow ===\n");
                var visualizationWorkflow = new VisualizationWorkflow(appConfig);
                await visualizationWorkflow.RunAsync();
                break;

            case "5":
                Console.WriteLine("=== Demo 5: Interactive Checkpointing ===\n");
                var checkpointingWorkflow = new InteractiveCheckpointingWorkflow(appConfig);
                await checkpointingWorkflow.RunAsync();
                break;

            case "6":
                Console.WriteLine("=== Demo 6: Agents in Workflow ===\n");
                var agentsWorkflow = new AgentsInWorkflow(appConfig);
                await agentsWorkflow.RunAsync();
                break;

            default:
                Console.WriteLine("Invalid choice. Please select a number from 1-6.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError running demo: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }

    Console.WriteLine("\n\nPress any key to return to the menu...");
    Console.ReadKey();
}

