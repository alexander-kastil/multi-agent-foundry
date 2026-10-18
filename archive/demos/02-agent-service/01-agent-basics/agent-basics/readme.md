# Azure AI Agent Service Basics

This C# console application demonstrates the core capabilities of Azure AI Agent Service, organized into multiple interactive demos.

## Demo Scripts Overview

| Demo                | Focus                                  | What It Demonstrates                                                                                                                                                                                                        |
| ------------------- | -------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Basics`            | Core lifecycle                         | Creates an agent, a thread, sends a user message, starts a run, polls status, and lists messages. Shows the minimal synchronous workflow for interacting with Azure AI Agents and how statuses transition until completion. |
| `EventHandler`      | Streaming events                       | Demonstrates polling-based status monitoring as an alternative to streaming. Shows how to check run progress iteratively and inspect status transitions without waiting for full completion.  |
| `ResponseFormat`    | Structured output                      | Configures the agent to return responses suitable for structured data (e.g. list of planets) for downstream parsing. Highlights response format control through instructions.      |
| `InputFile`         | File input (image)                     | Uploads a local image file to demonstrate file upload with purpose `AGENTS`. Note: Multimodal messages are handled differently in the C# SDK compared to Python.              |
| `InputUrl`          | URL image input                        | Demonstrates referencing external image resources. Note: The C# SDK handles image URLs differently than the Python SDK, so this demo adapts the approach.                                          |
| `InputBase64`       | Data URL image input                   | Demonstrates Base64 encoding of images. Note: The C# SDK does not support data URL images in the same way as Python, so this demo shows the encoding process.      |
| `Output`            | Post-processing & external integration | Runs an agent, captures its response, then generates a QR code and uploads it to Azure Blob Storage. Demonstrates chaining agent output into application logic and persisting artifacts externally.                         |

## Prerequisites

- .NET 10 SDK
- Azure AI Foundry project with configured AI Agent Service
- Azure credentials configured (via DefaultAzureCredential)
- Optional: Azure Storage account for the Output demo

## Configuration

Edit `appsettings.json` to configure your Azure resources:

| Setting                    | Description                                      | Required |
| -------------------------- | ------------------------------------------------ | -------- |
| `ProjectConnectionString`  | Azure AI Foundry project connection string       | Yes      |
| `Model`                    | Deployment name for the AI model (e.g. gpt-4o-mini) | Yes      |
| `StorageConnectionString`  | Azure Storage connection string                  | No*      |
| `StorageContainerName`     | Container name for blob storage                  | No*      |

*Required only for the Output demo

## Running the Application

1. Navigate to the project directory:
   ```bash
   cd demos/02-agent-service/01-agent-basics/agent-basics
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Select a demo from the interactive menu by entering a number (1-7)

5. After each demo completes, press any key to return to the menu

6. Press Ctrl+C to exit the application

## Key Concepts Demonstrated

- **Agent Lifecycle**: Creating agents, threads, messages, and managing run states
- **Synchronous Polling**: Checking run completion through status polling
- **File Upload**: Uploading files to Azure AI Agent Service
- **Response Processing**: Capturing and using agent responses
- **External Integration**: Post-processing agent responses and uploading to Azure Storage

## Implementation Notes

This C# implementation uses the `Azure.AI.Agents.Persistent` SDK which has some differences from the Python SDK:

- **Multimodal Messages**: The C# SDK does not support multimodal message content blocks (combining text and images) in the same way as Python. Demos 4-6 have been adapted to demonstrate file uploads and referencing, but not direct image analysis.
- **Streaming**: The C# SDK has a different streaming API. Demo 2 uses status polling instead of event streaming to demonstrate monitoring run progress.
- **Response Format**: JSON response formatting is controlled through agent instructions rather than explicit response format parameters.

Despite these differences, all core agent functionality is demonstrated, including agent creation, thread management, message handling, and external integration.

## Project Structure

```
agent-basics/
├── Models/
│   └── AppConfig.cs              # Configuration model
├── Services/
│   ├── AgentRunnerBasics.cs      # Core lifecycle demo
│   ├── AgentRunnerEventHandler.cs # Status monitoring demo
│   ├── AgentRunnerResponseFormat.cs # Structured output demo
│   ├── AgentRunnerInputFile.cs   # File upload demo
│   ├── AgentRunnerInputUrl.cs    # URL reference demo
│   ├── AgentRunnerInputBase64.cs # Base64 encoding demo
│   └── AgentRunnerOutput.cs      # External integration demo
├── assets/
│   └── soi.jpg                   # Sample image for demos
├── Program.cs                     # Main entry point with menu
├── appsettings.json               # Configuration file
└── agent-basics.csproj            # Project file
```
