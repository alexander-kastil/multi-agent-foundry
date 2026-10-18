# Azure AI Agent Orchestration - Incident Resolution (Python)

This is a multi-agent orchestration demo showing how to coordinate specialized agents to analyze and resolve system incidents from log files.

## Overview

This demo demonstrates:
- Multi-agent coordination using Azure AI Foundry
- Specialized agents with different roles and capabilities
- Custom function tools for log reading and DevOps operations
- Iterative problem-solving workflows
- Agent-to-agent communication patterns

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

- **`resolve_incident.py`** - Main orchestration application
- **`log_plugin.py`** - Plugin for reading log files
- **`devops_plugin.py`** - Plugin for DevOps operations (restart, rollback, etc.)
- **`sample_logs/`** - Sample log files demonstrating various issues
- **`logs/`** - Working directory for log files (created at runtime)
- **`requirements.txt`** - Python dependencies

## How It Works

### Agent Roles

1. **Incident Manager Agent**
   - Reads and analyzes log files
   - Identifies issues and patterns
   - Recommends corrective actions
   - Verifies if issues are resolved

2. **DevOps Assistant Agent**
   - Executes corrective actions
   - Updates log files with action results
   - Performs operations like:
     - Restart service
     - Rollback transaction
     - Redeploy resource
     - Increase quota
     - Escalate issue

### Workflow

1. Copy sample logs to working directory
2. For each log file:
   - Create a new conversation thread
   - Iterate up to 5 times or until resolved:
     - Incident Manager analyzes the log
     - Recommends a corrective action
     - DevOps Assistant executes the action
     - Incident Manager verifies the resolution
3. Clean up agents and threads

## Running the Demo

```bash
python resolve_incident.py
```

The orchestration will:
1. Process each log file in the `sample_logs/` directory
2. Display agent recommendations and actions
3. Show iteration progress
4. Report when issues are resolved or escalated

## Sample Log Files

The demo includes 4 sample log files demonstrating different scenarios:
- **log1.log** - Service connection failures requiring service restart
- **log2.log** - Transaction integrity issues requiring rollback
- **log3.log** - Resource deployment failures requiring redeployment
- **log4.log** - API quota exceeded requiring quota increase

## Key Features

### Multi-Agent Coordination
- Incident Manager focuses on analysis
- DevOps Assistant focuses on remediation
- Agents communicate through thread messages
- Each agent has specialized tools

### Iterative Resolution
- Maximum 5 iterations per incident
- Agents verify their own work
- Automatic retry on rate limits
- Graceful error handling

### Function Tools

**Log Plugin (`log_plugin.py`)**:
- `read_log_file(filepath)` - Reads and returns log file contents

**DevOps Plugin (`devops_plugin.py`)**:
- `restart_service(service_name, logfile)` - Restarts a service
- `rollback_transaction(logfile)` - Rolls back failed transactions
- `redeploy_resource(resource_name, logfile)` - Redeploys resources
- `increase_quota(logfile)` - Increases API quotas
- `escalate_issue(logfile)` - Escalates unresolvable issues

All DevOps functions append action logs to the original log file.

## Configuration

### Rate Limiting
- 15-second delay between iterations
- Automatic 60-second wait on rate limit errors
- Configurable via `delay` variable

### Iteration Limits
- Maximum 5 iterations per log file
- Configurable via `max_iterations` variable

## Notes

- This is a demonstration/training example
- DevOps functions simulate operations by updating log files
- Actual operations are not performed on real infrastructure
- Agents and threads are cleaned up after each log file
- Uses synchronous API for simplicity

## Related Examples

- C# version: `../af-azure-orchestration/`
- Simple agent example: `../af-azure-ai-agent-py/`

## Troubleshooting

**Rate limit errors**: The demo handles these automatically by waiting and retrying.

**Missing log files**: Sample logs are copied to the `logs/` directory at runtime.

**Agent failures**: Check that your Azure credentials are valid and the model deployment name is correct.
