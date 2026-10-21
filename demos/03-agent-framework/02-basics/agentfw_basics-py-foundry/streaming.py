"""
Streaming responses demo using Azure AI Foundry SDK.

This demo shows how to create an agent and demonstrate streaming responses
for real-time token-by-token output.
"""

import asyncio
import os
from dotenv import load_dotenv

from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.agents.aio import AgentsClient

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("PROJECT_ENDPOINT")
MODEL_DEPLOYMENT = os.getenv("MODEL_DEPLOYMENT")


async def main():
    """Interactive demo: Create agent and demonstrate streaming responses."""
    
    print("\n" + "="*70)
    print("DEMO: Response Streaming (Azure AI Foundry SDK)")
    print("="*70)
    
    async with DefaultAzureCredential() as credential:
        # Create the Azure AI Project client
        async with AIProjectClient(
            endpoint=PROJECT_ENDPOINT,
            credential=credential
        ) as project_client:
            
            # Create the agent using AgentsClient
            async with AgentsClient(
                endpoint=PROJECT_ENDPOINT,
                credential=credential
            ) as agents_client:
                
                print("\nCreating new agent in Azure AI Foundry...")
                
                agent = await agents_client.create_agent(
                    model=MODEL_DEPLOYMENT,
                    name="Streaming Demo Agent",
                    instructions=(
                        "You are a helpful AI assistant that provides detailed, "
                        "informative responses. Be thorough and engaging in your explanations."
                    ),
                    description="Agent demonstrating streaming capabilities"
                )
                
                print(f"Agent created successfully!")
                print(f"   Agent ID: {agent.id}")
                print(f"   Name: {agent.name}")
                
                # Create a thread for the conversation
                thread = await agents_client.create_thread()
                print(f"   Thread ID: {thread.id}")
                
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
                    
                    # Create message in thread
                    await agents_client.create_message(
                        thread_id=thread.id,
                        role="user",
                        content=user_input
                    )
                    
                    # Create and process run with streaming
                    print("\nAgent: ", end="", flush=True)
                    
                    try:
                        async with await agents_client.create_run_stream(
                            thread_id=thread.id,
                            assistant_id=agent.id
                        ) as stream:
                            async for event in stream:
                                # Handle text deltas for streaming output
                                if event.event == "thread.message.delta":
                                    if hasattr(event.data, 'delta') and hasattr(event.data.delta, 'content'):
                                        for content in event.data.delta.content:
                                            if hasattr(content, 'text') and hasattr(content.text, 'value'):
                                                print(content.text.value, end="", flush=True)
                        
                        print("\n")
                        
                    except Exception as e:
                        print(f"\nError during streaming: {e}\n")
                
                # Clean up: delete the agent
                print("\nCleaning up...")
                await agents_client.delete_agent(agent.id)
                print("Agent deleted.")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
    except Exception as e:
        print(f"\nUnexpected error: {e}")
