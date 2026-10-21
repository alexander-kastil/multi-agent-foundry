"""
Connect to and use an existing Azure AI Foundry Agent.

This demo shows how to connect to an agent that already exists
in Azure AI Foundry by using its ID.
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
AGENT_ID = os.getenv("AZURE_AI_AGENT_ID")


async def main():
    """Interactive demo: Connect to existing agent and chat."""
    
    print("\n" + "="*70)
    print("DEMO: Use Existing Azure AI Foundry Agent (Azure AI Foundry SDK)")
    print("="*70)
    
    if not AGENT_ID or AGENT_ID == "REPLACE_WITH_YOUR_VALUE":
        print("\nERROR: Please set AZURE_AI_AGENT_ID in your .env file")
        print("This demo requires an existing agent ID.")
        return
    
    async with DefaultAzureCredential() as credential:
        # Create the Azure AI Project client
        async with AIProjectClient(
            endpoint=PROJECT_ENDPOINT,
            credential=credential
        ) as project_client:
            
            # Connect to existing agent using AgentsClient
            async with AgentsClient(
                endpoint=PROJECT_ENDPOINT,
                credential=credential
            ) as agents_client:
                
                print(f"\nConnecting to existing agent: {AGENT_ID}")
                
                # Retrieve the agent
                agent = await agents_client.get_agent(AGENT_ID)
                
                print(f"Connected to agent successfully!")
                print(f"   Agent ID: {agent.id}")
                print(f"   Name: {agent.name}")
                print(f"   Model: {agent.model}")
                print(f"   Instructions: {agent.instructions[:100]}...")
                
                # Create a thread for the conversation
                thread = await agents_client.create_thread()
                print(f"\n   Thread ID: {thread.id}")
                
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
                    
                    # Create message in thread
                    await agents_client.create_message(
                        thread_id=thread.id,
                        role="user",
                        content=user_input
                    )
                    
                    # Create and process run with streaming
                    print("Agent: ", end="", flush=True)
                    
                    async with await agents_client.create_run_stream(
                        thread_id=thread.id,
                        assistant_id=agent.id
                    ) as stream:
                        async for event in stream:
                            if event.event == "thread.message.delta":
                                if hasattr(event.data, 'delta') and hasattr(event.data.delta, 'content'):
                                    for content in event.data.delta.content:
                                        if hasattr(content, 'text') and hasattr(content.text, 'value'):
                                            print(content.text.value, end="", flush=True)
                    
                    print("\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
    except Exception as e:
        print(f"\nUnexpected error: {e}")
