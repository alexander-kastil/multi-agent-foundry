import asyncio
import os
from pydantic import BaseModel
from dotenv import load_dotenv

from agent_framework.azure import AzureOpenAIChatClient

# Load environment variables
load_dotenv()

ENDPOINT = os.getenv("AZURE_OPENAI_ENDPOINT")
DEPLOYMENT = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")
API_KEY = os.getenv("AZURE_OPENAI_API_KEY")
API_VERSION = os.getenv("AZURE_OPENAI_API_VERSION", "2024-07-01-preview")


# Define structured output model
class PersonInfo(BaseModel):
    """Extract person information from text."""
    name: str | None = None
    age: int | None = None
    occupation: str | None = None
    city: str | None = None


async def main():
    """Interactive demo: Structured output extraction."""
    
    print("\n" + "="*70)
    print("DEMO: Structured Output with Pydantic")
    print("="*70)
    
    # Create agent
    agent = AzureOpenAIChatClient(
        endpoint=ENDPOINT,
        deployment_name=DEPLOYMENT,
        api_key=API_KEY,
        api_version=API_VERSION
    ).create_agent(
        instructions="Extract person information from the user's text.",
        name="ExtractorBot"
    )
    
    print("\nAgent created with PersonInfo schema")
    print("Schema: name, age, occupation, city")
    
    print("\n" + "="*70)
    print("Interactive Chat (Type 'quit' to exit)")
    print("="*70)
    print("\nTIP: Describe a person and see structured extraction\n")
    
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
        
        # Get structured response
        print("\nExtracting structured data...")
        response = await agent.run(user_input, response_format=PersonInfo)
        
        if response.value:
            person = response.value
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
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
