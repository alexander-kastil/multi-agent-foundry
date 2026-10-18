# Foundry SDK Python Demo

This demo shows how to use Azure AI Foundry SDK with Python.

## Chat Demos

| Demo                             | Description                                                                                                                                                  |
| -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `chat-foundry.py`                | Basic single-turn chat completion using a specific model with a custom system prompt                                                                         |
| `chat-model-router.py`           | Single-turn chat using Azure AI model router to automatically select the best model based on request characteristics                                         |
| `chat-model-router-multiturn.py` | Interactive multi-turn chat session with model router, maintaining conversation history with configurable parameters (temperature, max tokens, history size) |
| `chat-function-calling.py`       | Agent-based chat with function calling capability to lookup weather information using tools                                                                  |

## Setup

1. Install dependencies:

   ```bash
   uv sync
   ```

2. Copy the template and configure your environment:

   ```bash
   cp .env.copy .env
   ```

3. Edit `.env` and set your `PROJECT_ENDPOINT`, `OPENAI_API_VERSION`, and `MODEL`:

   ```
   PROJECT_ENDPOINT=https://your-ai-services-account-name.services.ai.azure.com/api/projects/your-project-name
   OPENAI_API_VERSION=2024-02-01
   MODEL="gpt-4.1-mini"
   ```

4. Run the demos:
   ```bash
   uv run python chat-foundry.py
   uv run python prompt-templates.py
   ```
