# Develop an AI agent with Azure AI Agent Service

[Develop an AI agent with Azure AI Agent Service](https://learn.microsoft.com/en-us/training/modules/develop-ai-agent-azure/)

| File                       | Description                                                                       |
| -------------------------- | --------------------------------------------------------------------------------- |
| `agent-basics.py`          | Main script to create and run the agent.                                          |
| `agent-event-handler.py`   | Custom event handler for processing agent events.                                 |
| `agent-response-format.py` | Defines the response format for the agent, which is JSON in this case             |
| `agent-input-url.py`       | Handles URL input for the agent. Analyzes an image                                |
| `agent-input-file.py`      | Handles file input for the agent. Analyzes an image                               |
| `agent-input-base64.py`    | Handles base64 encoded input for the agent. Analyzes an image                     |
| `agent-output.py`          | Takes an input, encodes it in a QR code and stores it in an Azure Storage account |
