# Azure AI Agent - Expense Claim Submission (Python)

This is a minimal Python implementation demonstrating how to create an Azure AI agent using the Azure AI Projects SDK with function calling capabilities.

## Overview

This demo shows how to:
- Create an AI agent using Azure AI Foundry
- Implement custom function tools (plugins)
- Process user requests through an agent
- Handle function calling to send emails

## Prerequisites

- Python 3.8 or higher
- Azure AI Foundry project with a deployed model
- Azure credentials configured (uses `DefaultAzureCredential`)

## Setup

1. Install dependencies:
```bash
pip install -r requirements.txt
```

2. Configure environment variables in `.env`:
```
PROJECT_ENDPOINT="https://your-project.services.ai.azure.com/api/projects/your-project"
MODEL_DEPLOYMENT="gpt-4o-mini"
```

## Project Structure

- **`claim_submission.py`** - Main application that creates an expense claim agent
- **`email_plugin.py`** - Custom function plugin for sending emails
- **`requirements.txt`** - Python dependencies
- **`pyproject.toml`** - Project metadata

## How It Works

1. The application creates expense claim data with date, description, and amount
2. Creates an Azure AI agent with instructions to process expense claims
3. Attaches a custom `send_email` function as a tool
4. Sends a user message requesting an expense claim
5. The agent processes the request and calls the email function
6. Returns a confirmation to the user

## Running the Demo

```bash
python claim_submission.py
```

The agent will:
1. Parse the expense data
2. Format it into an itemized list with totals
3. Call the `send_email` function to send to expenses@contoso.com
4. Confirm the action to the user

## Key Components

### Agent Configuration
- **Model**: Configurable via environment variable
- **Tools**: Function calling for email operations
- **Instructions**: Pre-defined behavior for expense claim processing

### Function Plugin
The `email_plugin.py` demonstrates:
- Function definition with type hints and docstrings
- Parameter handling (to, subject, body)
- Return value formatting

## Notes

- This is a demonstration/training example
- The `send_email` function prints to console instead of actually sending emails
- Agent and thread are cleaned up automatically after execution
- Uses synchronous API for simplicity

## Related Examples

- C# version: `../af-azure-ai-agent/`
- Orchestration example: `../af-azure-orchestration-py/`
