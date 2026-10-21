"""
Structured output extraction demo using Azure AI Foundry SDK.

This demo shows how to extract structured data using Pydantic models
with Azure OpenAI's structured outputs feature.
"""

import os
from pydantic import BaseModel
from dotenv import load_dotenv

from azure.ai.inference import ChatCompletionsClient
from azure.ai.inference.models import SystemMessage, UserMessage
from azure.core.credentials import AzureKeyCredential

# Load environment variables
load_dotenv()

ENDPOINT = os.getenv("AZURE_OPENAI_ENDPOINT")
DEPLOYMENT = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")
API_KEY = os.getenv("AZURE_OPENAI_API_KEY")


# Define structured output model
class PersonInfo(BaseModel):
    """Extract person information from text."""
    name: str | None = None
    age: int | None = None
    occupation: str | None = None
    city: str | None = None


def main():
    """Interactive demo: Structured output extraction."""
    
    print("\n" + "="*70)
    print("DEMO: Structured Output with Pydantic (Azure AI Foundry SDK)")
    print("="*70)
    
    # Create chat client
    client = ChatCompletionsClient(
        endpoint=ENDPOINT,
        credential=AzureKeyCredential(API_KEY)
    )
    
    print("\nChat client created with PersonInfo schema")
    print("Schema: name, age, occupation, city")
    
    print("\n" + "="*70)
    print("Interactive Chat (Type 'quit' to exit)")
    print("="*70)
    print("\nTIP: Describe a person and see structured extraction")
    print("Example: 'John is a 35-year-old software engineer living in Seattle'\n")
    
    while True:
        try:
            user_input = input("You: ")
        except EOFError:
            print("\nReceived EOF - exiting.")
            break
        except KeyboardInterrupt:
            print("\nInterrupted - exiting.")
            break
        
        if user_input.lower() in ['quit', 'exit', 'q']:
            print("\nGoodbye!")
            break
        
        if not user_input.strip():
            continue
        
        # Prepare messages for extraction
        messages = [
            SystemMessage(content="Extract person information from the user's text. Return the data in JSON format matching the PersonInfo schema."),
            UserMessage(content=user_input)
        ]
        
        print("\nExtracting structured data...")
        
        # Get structured response using response_format
        response = client.complete(
            model=DEPLOYMENT,
            messages=messages,
            response_format={
                "type": "json_schema",
                "json_schema": {
                    "name": "PersonInfo",
                    "strict": True,
                    "schema": PersonInfo.model_json_schema()
                }
            }
        )
        
        if response.choices:
            content = response.choices[0].message.content
            
            # Parse the JSON response into Pydantic model
            import json
            person_data = json.loads(content)
            person = PersonInfo(**person_data)
            
            print("\nExtracted Information:")
            print(f"   Name: {person.name}")
            print(f"   Age: {person.age}")
            print(f"   Occupation: {person.occupation}")
            print(f"   City: {person.city}")
        else:
            print("Could not extract information")
        
        print()


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\nSee you again soon.")
