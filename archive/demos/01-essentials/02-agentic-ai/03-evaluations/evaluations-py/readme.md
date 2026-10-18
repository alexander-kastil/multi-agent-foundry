# Azure AI Foundry Evaluations Demo

This demo shows how to use Azure AI Foundry SDK for content safety evaluations.

## Setup

1. Install dependencies:

   ```bash
   uv sync
   ```

2. Copy the template and configure your environment:

   ```bash
   cp .env.copy .env
   ```

3. Edit `.env` and set your project details:

   ```
   PROJECT_ENDPOINT=https://your-ai-services-account-name.services.ai.azure.com/api/projects/your-project-name
   AZURE_SUBSCRIPTION_ID=your-subscription-id
   AZURE_RESOURCE_GROUP=your-resource-group-name
   AZURE_PROJECT_NAME=your-project-name
   ```

4. Run the evaluation:
   ```bash
   uv run python evaluation.py
   ```
