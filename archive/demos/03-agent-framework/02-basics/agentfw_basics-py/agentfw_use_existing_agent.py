import asyncio
import os
from dotenv import load_dotenv

from agent_framework import ChatAgent
from agent_framework.azure import AzureAIAgentClient
from azure.identity.aio import AzureCliCredential

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("AZURE_AI_PROJECT_ENDPOINT")
AGENT_ID = os.getenv("AZURE_AI_AGENT_ID")


async def main():
    """Interactive demo: Connect to existing agent."""
    
    print("\n" + "="*70)
    print("DEMO: Connect to Existing Azure AI Foundry Agent")
    print("="*70)
    
    print(f"\nConnecting to agent: {AGENT_ID}")
    
    async with (
        AzureCliCredential() as credential,
        ChatAgent(
            chat_client=AzureAIAgentClient(
                async_credential=credential,
                project_endpoint=PROJECT_ENDPOINT,
                agent_id=AGENT_ID
            )
        ) as agent
    ):
        print("Connected successfully!")
        
        print("\n" + "="*70)
        print("Interactive Chat (Type 'quit' to exit)")
        print("="*70 + "\n")
        
        while True:
            # Get user input
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
            
            # Get streaming response
            print("Agent: ", end="", flush=True)
            async for chunk in agent.run_stream(user_input):
                if chunk.text:
                    print(chunk.text, end="", flush=True)
            print("\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
