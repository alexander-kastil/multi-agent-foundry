using AgentFwWorkflows.Models;
using System.Text;

namespace AgentFwWorkflows.Workflows;

public class VisualizationWorkflow : WorkflowRunnerBase
{
    public VisualizationWorkflow(AppConfig config) : base(config)
    {
    }

    public override async Task RunAsync()
    {
        PrintHeader(
            "WORKFLOW VISUALIZATION - INVOICE BUILDER",
            @"This demo visualizes different workflow patterns:
  • Sequential Workflow - Linear processing
  • Parallel Workflow - Concurrent processing with fan-out/fan-in
  • Branching Workflow - Conditional routing with switch-case

Output formats:
  • Mermaid (for Markdown and web rendering)
  • Text-based diagrams for console display"
        );

        InvoiceUtils.EnsureDirectories(Path.Combine(OutputPath, "visualizations"));

        var selectedPatterns = ShowWorkflowMenu();

        Console.WriteLine($"\nSelected patterns: {string.Join(", ", selectedPatterns)}");
        InvoiceUtils.WaitForUser("start visualization");

        foreach (var pattern in selectedPatterns)
        {
            Console.WriteLine($"\n\n{new string('█', 80)}");
            Console.WriteLine($"█{new string(' ', 78)}█");
            Console.WriteLine($"█{pattern.ToUpper().PadLeft(40 + pattern.Length / 2).PadRight(78)}█");
            Console.WriteLine($"█{new string(' ', 78)}█");
            Console.WriteLine($"█{new string(' ', 78)}█");
            Console.WriteLine($"{new string('█', 80)}");

            await VisualizePatternAsync(pattern);
        }

        Console.WriteLine("\n\n" + new string('=', 80));
        Console.WriteLine("VISUALIZATION COMPLETE");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"\nOutput Directory: {Path.Combine(OutputPath, "visualizations")}");
        Console.WriteLine("\nGenerated Files:");

        foreach (var pattern in selectedPatterns)
        {
            Console.WriteLine($"  • {pattern}_workflow.mmd (Mermaid)");
            Console.WriteLine($"  • {pattern}_workflow.txt (Text)");
        }

        Console.WriteLine("\nUsage Tips:");
        Console.WriteLine("  • Copy .mmd files to Mermaid Live Editor (https://mermaid.live)");
        Console.WriteLine("  • Visual diagrams help understand complex workflows");
        Console.WriteLine("  • Great for documentation and presentations");

        Console.WriteLine("\nWorkflow Pattern Summary:");
        if (selectedPatterns.Contains("sequential"))
            Console.WriteLine("  Sequential: Best for step-by-step processing");
        if (selectedPatterns.Contains("parallel"))
            Console.WriteLine("  Parallel: Best for independent concurrent tasks");
        if (selectedPatterns.Contains("branching"))
            Console.WriteLine("  Branching: Best for conditional routing based on data");

        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("All selected workflow patterns visualized successfully!");
        Console.WriteLine(new string('=', 80));
    }

    private List<string> ShowWorkflowMenu()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("WORKFLOW VISUALIZATION OPTIONS");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("Select which workflow patterns to visualize:");
        Console.WriteLine();
        Console.WriteLine("1. Sequential Workflow");
        Console.WriteLine("   • Linear processing (A -> B -> C -> D)");
        Console.WriteLine("   • Best for step-by-step operations");
        Console.WriteLine();
        Console.WriteLine("2. Parallel Workflow");
        Console.WriteLine("   • Concurrent processing with fan-out/fan-in");
        Console.WriteLine("   • Best for independent concurrent tasks");
        Console.WriteLine();
        Console.WriteLine("3. Branching Workflow");
        Console.WriteLine("   • Conditional routing with switch-case");
        Console.WriteLine("   • Best for conditional routing based on data");
        Console.WriteLine();
        Console.WriteLine("4. All Workflows (Complete Demo)");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Enter your selection (1-4, or comma-separated like '1,3'): ");
            var choice = Console.ReadLine()?.Trim();

            if (choice == "4")
            {
                return new List<string> { "sequential", "parallel", "branching" };
            }

            var choices = choice?.Split(',').Select(c => c.Trim()).ToList() ?? new List<string>();
            var patterns = new List<string>();

            foreach (var c in choices)
            {
                if (c == "1") patterns.Add("sequential");
                else if (c == "2") patterns.Add("parallel");
                else if (c == "3") patterns.Add("branching");
                else
                {
                    Console.WriteLine($"Invalid choice: {c}. Please enter 1, 2, 3, or 4.");
                    break;
                }
            }

            if (patterns.Count > 0) return patterns;
            Console.WriteLine("Please select at least one pattern.");
        }
    }

    private async Task VisualizePatternAsync(string pattern)
    {
        Console.WriteLine($"\n{new string('=', 80)}");
        Console.WriteLine($"VISUALIZATION: {pattern.ToUpper()} WORKFLOW");
        Console.WriteLine(new string('=', 80));

        await Task.Delay(100);

        var (mermaid, textDiagram, analysis) = pattern switch
        {
            "sequential" => GenerateSequentialVisualization(),
            "parallel" => GenerateParallelVisualization(),
            "branching" => GenerateBranchingVisualization(),
            _ => ("", "", "")
        };

        Console.WriteLine("\nMermaid Diagram:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine(mermaid);
        Console.WriteLine(new string('-', 80));

        Console.WriteLine("\nText Diagram:");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine(textDiagram);
        Console.WriteLine(new string('-', 80));

        Console.WriteLine($"\n{analysis}");

        // Save files
        var vizDir = Path.Combine(OutputPath, "visualizations");
        Directory.CreateDirectory(vizDir);

        var mermaidFile = Path.Combine(vizDir, $"{pattern}_workflow.mmd");
        var textFile = Path.Combine(vizDir, $"{pattern}_workflow.txt");

        await File.WriteAllTextAsync(mermaidFile, mermaid);
        await File.WriteAllTextAsync(textFile, textDiagram + "\n\n" + analysis);

        Console.WriteLine($"\nSaved Mermaid: {mermaidFile}");
        Console.WriteLine($"Saved Text: {textFile}");

        InvoiceUtils.WaitForUser("continue to next visualization");
    }

    private (string, string, string) GenerateSequentialVisualization()
    {
        var mermaid = @"graph LR
    A[Load Invoices] --> B[Calculate Totals]
    B --> C[Render Invoices]
    C --> D[Save Invoices]
    
    style A fill:#e1f5ff
    style B fill:#fff4e1
    style C fill:#ffe1f5
    style D fill:#e1ffe1";

        var textDiagram = @"
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐    ┌──────────────────┐
│ Load Invoices   │───▶│ Calculate Totals │───▶│ Render Invoices │───▶│  Save Invoices   │
└─────────────────┘    └──────────────────┘    └─────────────────┘    └──────────────────┘
     Step 1                   Step 2                  Step 3                  Step 4
";

        var analysis = @"Analysis: Sequential Workflow
─────────────────────────────────────────────────────────────────────────────────
Pattern: Linear chain (A -> B -> C -> D)
Parallelism: None
Branches: None
Use Case: Step-by-step processing where each step depends on the previous

Executors:
  1. loader (entry point)
  2. calculator
  3. renderer
  4. saver (exit point)";

        return (mermaid, textDiagram, analysis);
    }

    private (string, string, string) GenerateParallelVisualization()
    {
        var mermaid = @"graph TD
    A[Dispatcher] --> B[Totals Calculator]
    A --> C[Client Preparer]
    B --> D[Merger]
    C --> D
    D --> E[Renderer]
    
    style A fill:#e1f5ff
    style B fill:#fff4e1
    style C fill:#fff4e1
    style D fill:#ffe1f5
    style E fill:#e1ffe1";

        var textDiagram = @"
                              ┌──────────────────────┐
                          ┌──▶│  Totals Calculator   │──┐
┌─────────────┐           │   └──────────────────────┘  │    ┌─────────────┐    ┌────────────┐
│  Dispatcher │───────────┤                             ├───▶│   Merger    │───▶│  Renderer  │
└─────────────┘           │   ┌──────────────────────┐  │    └─────────────┘    └────────────┘
                          └──▶│   Client Preparer    │──┘
                              └──────────────────────┘
                                 Parallel Execution
";

        var analysis = @"Analysis: Parallel Workflow
─────────────────────────────────────────────────────────────────────────────────
Pattern: Fan-out/Fan-in
Parallelism: 2 concurrent branches
Branches: None
Use Case: Independent concurrent tasks that can run simultaneously

Executors:
  1. dispatcher (entry point)
  2. totals_calculator (parallel)
  3. client_preparer (parallel)
  4. merger (synchronization)
  5. renderer (exit point)";

        return (mermaid, textDiagram, analysis);
    }

    private (string, string, string) GenerateBranchingVisualization()
    {
        var mermaid = @"graph TD
    A[Analyzer] -->|High Value| B[High Value Handler]
    A -->|Preferred| C[Preferred Handler]
    A -->|Default| D[Standard Handler]
    B --> E[Finalizer]
    C --> E
    D --> E
    
    style A fill:#e1f5ff
    style B fill:#fff4e1
    style C fill:#fff4e1
    style D fill:#fff4e1
    style E fill:#e1ffe1";

        var textDiagram = @"
                              ┌──────────────────────┐
                          ┌──▶│ High Value Handler   │──┐
                          │   └──────────────────────┘  │
┌─────────────┐           │                             │    ┌─────────────┐
│  Analyzer   │──────────┼──▶│  Preferred Handler   │──┼───▶│  Finalizer  │
└─────────────┘           │   └──────────────────────┘  │    └─────────────┘
                          │                             │
                          └──▶│  Standard Handler    │──┘
                              └──────────────────────┘
                                Conditional Routing
";

        var analysis = @"Analysis: Branching Workflow
─────────────────────────────────────────────────────────────────────────────────
Pattern: Switch-case with convergence
Parallelism: None
Branches: 3 conditional paths
Use Case: Conditional routing based on data or business rules

Executors:
  1. analyzer (entry point)
  2. high_value_handler (conditional)
  3. preferred_handler (conditional)
  4. standard_handler (default)
  5. finalizer (convergence point)";

        return (mermaid, textDiagram, analysis);
    }
}
