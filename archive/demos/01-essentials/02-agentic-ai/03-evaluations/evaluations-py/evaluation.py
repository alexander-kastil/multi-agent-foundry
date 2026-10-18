from pathlib import Path
from azure.ai.evaluation import ContentSafetyEvaluator, GroundednessEvaluator, RelevanceEvaluator
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
import os

# load environment variables from the .env file
from dotenv import load_dotenv
load_dotenv()

# Resolve credential once so both client and evaluator share it
credential = DefaultAzureCredential()

project_endpoint = os.environ["PROJECT_ENDPOINT"]
eval_model = os.environ["EVAL_MODEL"]
azure_openai_endpoint = os.environ["AZURE_OPENAI_ENDPOINT"]

# Create project client (useful when you want to interact with the project later)
project_client = AIProjectClient(endpoint=project_endpoint, credential=credential)

# Prefer endpoint-based scope for non hub-based projects; fall back to hub fields if needed
azure_ai_project_scope = project_endpoint or {
    "subscription_id": os.environ["AZURE_SUBSCRIPTION_ID"],
    "resource_group_name": os.environ["AZURE_RESOURCE_GROUP"],
    "project_name": os.environ["AZURE_PROJECT_NAME"],
}

# Create model configuration for evaluators
model_config = {
    "azure_deployment": eval_model,
    "azure_endpoint": azure_openai_endpoint,
    # Use GA chat completions API for widest compatibility
    "api_version": "2024-10-21",
}

# instantiate an evaluator with image and multi-modal support
safety_evaluator = ContentSafetyEvaluator(
    credential=credential,
    azure_ai_project=azure_ai_project_scope,
)

# instantiate groundedness evaluator
groundedness_evaluator = GroundednessEvaluator(
    model_config=model_config,
    credential=credential,
)

# instantiate relevance evaluator
relevance_evaluator = RelevanceEvaluator(
    model_config=model_config,
    credential=credential,
)

# Quick diagnostics
print(f"[eval] using deployment='{eval_model}' endpoint='{azure_openai_endpoint}' api_version='{model_config['api_version']}'")

# example of a conversation with an image URL
conversation_image_url = {
    "messages": [
        {
            "role": "system",
            "content": [
                {"type": "text", "text": "You are an AI assistant that understands images."}
            ],
        },
        {
            "role": "user",
            "content": [
                {"type": "text", "text": "Can you describe this image?"},
                {
                    "type": "image_url",
                    "image_url": {
                        "url": "https://www.integrations.at/media/1133/9f8cc0e6-2134-4829-a5da-3564e631d729.jpeg"
                    },
                },
            ],
        },
        {
            "role": "assistant",
            "content": [
                {
                    "type": "text",
                    "text": "The image shows a person with short dark hair wearing a blue checkered shirt. The background appears to be a wall with shadows cast on it",
                }
            ],
        },
    ]
}

if __name__ == "__main__":
    # Safety evaluation
    print("=== Content Safety Evaluation ===")
    safety_score = safety_evaluator(conversation=conversation_image_url)

    # Print all values from the safety evaluation
    for category, score in safety_score.items():
        print(f"{category}: {score}")
    
    # Extract components for quality evaluations
    query = "Can you describe this image?"
    response = "The image shows a person with short dark hair wearing a blue checkered shirt. The background appears to be a wall with shadows cast on it"
    context = "You are an AI assistant that understands images."
    
    # Groundedness evaluation
    print("\n=== Groundedness Evaluation ===")
    groundedness_score = groundedness_evaluator(
        query=query,
        response=response,
        context=context
    )
    print(f"Groundedness: {groundedness_score}")
    
    # Relevance evaluation
    print("\n=== Relevance Evaluation ===")
    relevance_score = relevance_evaluator(
        query=query,
        response=response,
        context=context
    )
    print(f"Relevance: {relevance_score}")