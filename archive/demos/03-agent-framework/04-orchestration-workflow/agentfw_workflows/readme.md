# Agent Framework Workflows (C#)

## Overview

This C# console application demonstrates various workflow orchestration patterns for invoice processing. It is a port of the Python `agentfw_workflows-py` implementation, adapted to use C# async/await patterns since there is no direct C# equivalent of the Python Agent Framework's workflow orchestration features.

All samples share common functionality through the `Models/InvoiceUtils.cs` helper class, which provides:

- **Invoice data model** (`InvoiceData`) with CSV loading
- **Configuration management** (`InvoiceConfig`) for tax rates, discounts, and thresholds
- **Calculation utilities** for subtotals, discounts (high-value and preferred client), and taxes
- **Rendering and I/O** for formatting invoices and saving to `output/invoices/`
- **Logging and archiving** with structured output to `output/logs/` and `output/archive/`

Each workflow demonstrates different orchestration patterns while processing the same invoice data, allowing for direct comparison of approaches.

## Configuration

The application reads configuration from `appsettings.json`:

```json
{
  "AzureAIProjectEndpoint": "https://<your-resource>.services.ai.azure.com/api/projects/<your-project>",
  "AzureAIModelDeploymentName": "gpt-4o-mini",
  "DataPath": "./data",
  "OutputPath": "./output"
}
```

Additional invoice configuration can be set via environment variables:
- `INVOICE_TAX_RATE` (default: 0.10)
- `INVOICE_HIGH_VALUE_THRESHOLD` (default: 5000.00)
- `INVOICE_HIGH_VALUE_DISCOUNT` (default: 0.05)
- `INVOICE_PREFERRED_DISCOUNT` (default: 0.03)
- `INVOICE_COMPANY_NAME` (default: "TechServices Inc.")
- `INVOICE_COMPANY_ADDRESS` (default: "123 Business St, Tech City, TC 12345")

## Running the Application

```bash
cd demos/03-agent-framework/04-orchestration-workflow/agentfw_workflows
dotnet run
```

The application will display an interactive menu where you can select from 6 different workflow demonstrations.

## Workflow Samples

| Name | Class | Description |
|------|-------|-------------|
| Sequential Workflow | `SequentialWorkflow.cs` | Linear 5-step workflow: configuration → invoice selection → calculation → rendering → saving. **Contains user pauses between steps, requiring manual ENTER presses.** |
| Concurrent Workflow | `ConcurrentWorkflow.cs` | Demonstrates parallel processing using `Task.WhenAll`. Dispatcher splits work into 3 concurrent tasks (totals calculator, client preparer, credit checker) that execute simultaneously, then merges results for final rendering. |
| Branching Workflow | `BranchingWorkflow.cs` | Conditional routing workflow with multiple decision points: checks for existing files to archive, applies discounts based on invoice value and client status (high-value/preferred/standard branches). |
| Visualization Workflow | `VisualizationWorkflow.cs` | Interactive workflow pattern visualizer. Generates Mermaid diagrams (`.mmd` files) and text-based diagrams for sequential, parallel, and branching patterns. Outputs to `output/visualizations/`. |
| Interactive Checkpointing | `InteractiveCheckpointingWorkflow.cs` | Human-in-the-loop workflow with state tracking. Pauses for user confirmation on tax rates and discounts, demonstrates checkpoint concept with state management. |
| Agents in Workflow | `AgentsInWorkflow.cs` | Demonstrates where AI agents could be integrated into workflow steps for intelligent processing: invoice analysis, business decisions, client communications, and executive summaries. **Note: Includes simulated agent behavior; full Azure AI integration requires proper credentials.** |

## Project Structure

```
agentfw_workflows/
├── Program.cs                          # Main menu and application entry point
├── appsettings.json                    # Application configuration
├── data/
│   └── invoices.csv                    # Sample invoice data
├── Models/
│   ├── AppConfig.cs                    # Application configuration model
│   ├── InvoiceConfig.cs                # Invoice processing configuration
│   ├── InvoiceData.cs                  # Invoice data model
│   ├── InvoiceTotals.cs                # Invoice calculation results
│   └── InvoiceUtils.cs                 # Shared utility functions
└── Workflows/
    ├── WorkflowRunnerBase.cs           # Base class for all workflows
    ├── SequentialWorkflow.cs           # Workflow 1: Sequential processing
    ├── ConcurrentWorkflow.cs           # Workflow 2: Parallel processing
    ├── BranchingWorkflow.cs            # Workflow 3: Conditional routing
    ├── VisualizationWorkflow.cs        # Workflow 4: Pattern visualization
    ├── InteractiveCheckpointingWorkflow.cs  # Workflow 5: Checkpointing
    └── AgentsInWorkflow.cs             # Workflow 6: AI agent integration
```

## Implementation Notes

### Differences from Python Version

Since the Microsoft Agent Framework is Python-specific and doesn't have a direct C# equivalent for workflow orchestration, this implementation:

1. **Uses C# async/await patterns** instead of the Python Agent Framework's workflow builder
2. **Demonstrates concepts** through standard C# practices (classes, async methods, Task-based concurrency)
3. **Maintains the same workflow patterns** (sequential, concurrent, branching) using C# idioms
4. **Provides the same educational value** while being idiomatic C#

### Workflow Patterns Demonstrated

- **Sequential Processing**: Simple async method chaining
- **Parallel Processing**: `Task.WhenAll` for concurrent execution
- **Conditional Routing**: Switch-case statements and conditional logic
- **State Management**: Simple state objects passed between workflow steps
- **User Interaction**: Console input/output for human-in-the-loop scenarios

## Output

All workflows generate output in the following directories:
- `output/invoices/` - Rendered invoice files
- `output/logs/` - Workflow execution logs
- `output/archive/` - Archived invoices (branching workflow)
- `output/visualizations/` - Workflow diagrams (visualization workflow)
- `output/interactive/` - Checkpointed invoices (interactive workflow)

## Development

### Building

```bash
dotnet build
```

### Running

```bash
dotnet run
```

### Adding New Workflows

1. Create a new class in the `Workflows/` folder that inherits from `WorkflowRunnerBase`
2. Implement the `RunAsync()` method
3. Add a menu item in `Program.cs`

## Related Demos

This demo is part of Module 3: Agent Framework in the Pro Code Agents training:
- **01-intro**: Introduction to Azure AI Agents
- **02-basics**: Agent Framework Basics
- **03-tools-knowledges**: Tools and Knowledge in Agent Framework
- **04-orchestration-workflow**: Workflow Orchestration Patterns (this demo)

For the Python version of these workflows using the Microsoft Agent Framework, see:
- `demos/03-agent-framework/04-orchestration-workflow/agentfw_workflows-py/`
