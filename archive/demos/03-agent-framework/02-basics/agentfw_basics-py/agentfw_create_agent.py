import asyncio
import os
from dotenv import load_dotenv

from agent_framework import ChatAgent
from agent_framework.azure import AzureAIAgentClient
from azure.identity.aio import AzureCliCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.agents.aio import AgentsClient

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("AZURE_AI_PROJECT_ENDPOINT")
MODEL_DEPLOYMENT = os.getenv("AZURE_AI_MODEL_DEPLOYMENT_NAME")


async def main():
    """Interactive demo: Create agent and chat."""
    
    print("\n" + "="*70)
    print("DEMO: Create Azure AI Foundry Agent (Interactive)")
    print("="*70)
    
    async with AzureCliCredential() as credential:
        # Create the agent in Azure AI Foundry
        async with AIProjectClient(
            endpoint=PROJECT_ENDPOINT,
            credential=credential
        ) as project_client:
            
            print("\nCreating new agent in Azure AI Foundry...")
            
            # NOTE: In recent azure-ai-projects previews, the .agents surface may require a different signature.
            # Use azure.ai.agents.AgentsClient for a stable create_agent API.
            async with AgentsClient(endpoint=PROJECT_ENDPOINT, credential=credential) as agents_client:
                created_agent = await agents_client.create_agent(
                model=MODEL_DEPLOYMENT,
                name="First AFW Agent",
                instructions="You are a helpful AI assistant. Be concise and friendly.",
                description="Created by "
            )
            
            print(f"Agent created successfully!")
            print(f"   Agent ID: {created_agent.id}")
            
            # Now use the agent for chat
            async with ChatAgent(
                chat_client=AzureAIAgentClient(
                    project_client=project_client,
                    agent_id=created_agent.id,
                    async_credential=credential
                )
            ) as agent:
                
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
                    
                    # Get response from agent
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
    except Exception as e:
        print(f"\nUnexpected error: {e}")
