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
    """Interactive demo: Create agent and demonstrate streaming responses."""
    
    print("\n" + "="*70)
    print("DEMO: Response Streaming with Agent Framework")
    print("="*70)
    
    async with AzureCliCredential() as credential:
        # Create the agent in Azure AI Foundry
        async with AIProjectClient(
            endpoint=PROJECT_ENDPOINT,
            credential=credential
        ) as project_client:
            
            print("\nCreating new agent in Azure AI Foundry...")
            
            async with AgentsClient(endpoint=PROJECT_ENDPOINT, credential=credential) as agents_client:
                created_agent = await agents_client.create_agent(
                    model=MODEL_DEPLOYMENT,
                    name="Streaming Demo Agent",
                    instructions=(
                        "You are a helpful AI assistant that provides detailed, "
                        "informative responses. Be thorough and engaging in your explanations."
                    ),
                    description="Agent demonstrating streaming capabilities"
                )
            
            print(f"Agent created successfully!")
            print(f"   Agent ID: {created_agent.id}")
            print(f"   Name: {created_agent.name}")
            
            # Use the agent for streaming chat
            async with ChatAgent(
                chat_client=AzureAIAgentClient(
                    project_client=project_client,
                    agent_id=created_agent.id,
                    async_credential=credential
                )
            ) as agent:
                
                print("\n" + "="*70)
                print("Interactive Streaming Chat")
                print("="*70)
                print("\nTIP: Responses will stream in real-time, token by token")
                print("TIP: Try asking for longer responses to see streaming in action")
                print("Examples:")
                print("   - 'Explain how neural networks work'")
                print("   - 'Tell me a creative story about a robot'")
                print("   - 'Describe the process of photosynthesis'")
                print("\n" + "="*70)
                print("Type 'quit' to exit\n")
                
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
                    print("\nAgent: ", end="", flush=True)
                    
                    try:
                        # Stream response token by token
                        async for chunk in agent.run_stream(user_input):
                            if chunk.text:
                                print(chunk.text, end="", flush=True)
                        
                        print("\n")
                        
                    except Exception as e:
                        print(f"\nError during streaming: {e}\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
    except Exception as e:
        print(f"\nUnexpected error: {e}")
