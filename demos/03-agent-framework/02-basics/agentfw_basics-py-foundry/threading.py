"""
Thread management and persistence demo using Azure AI Foundry SDK.

This demo shows how to manage conversation threads with automatic
serialization and deserialization for persistence.
"""

import asyncio
import os
import json
from datetime import datetime
from pathlib import Path
from dotenv import load_dotenv

from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.agents.aio import AgentsClient

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("PROJECT_ENDPOINT")
MODEL_DEPLOYMENT = os.getenv("MODEL_DEPLOYMENT")

# File to save thread history
THREAD_FILE = "thread_history.json"


async def main():
    """Interactive demo with thread serialization after every message."""
    
    print("\n" + "="*70)
    print("DEMO: Thread Management with Persistence (Azure AI Foundry SDK)")
    print("="*70)
    print("Demo Guide:")
    print("  1. Type a message (e.g. 'I am Alex')")
    print("  2. Agent responds using current thread context")
    print("  3. Thread info saves to 'thread_history.json'")
    print("  4. On restart, thread can be restored")
    print("  5. Type 'quit' to exit the demo")
    print("="*70)
    
    async with DefaultAzureCredential() as credential:
        async with AIProjectClient(
            endpoint=PROJECT_ENDPOINT,
            credential=credential
        ) as project_client:
            
            async with AgentsClient(
                endpoint=PROJECT_ENDPOINT,
                credential=credential
            ) as agents_client:
                
                print("\nCreating agent...")
                
                agent = await agents_client.create_agent(
                    model=MODEL_DEPLOYMENT,
                    name="Memory Bot",
                    instructions="You are a helpful assistant. Remember everything the user tells you and refer back to it.",
                    description="Agent with thread persistence"
                )
                
                print(f"Agent created: {agent.id}")
                
                # Try to load existing thread from file
                thread = None
                message_count = 0
                
                if Path(THREAD_FILE).exists():
                    try:
                        print(f"\nFound {THREAD_FILE}. Loading previous thread...")
                        with open(THREAD_FILE, 'r', encoding='utf-8') as f:
                            thread_data = json.load(f)
                        
                        thread_id = thread_data.get('thread_id')
                        message_count = thread_data.get('message_count', 0)
                        
                        # Try to retrieve the existing thread
                        thread = await agents_client.get_thread(thread_id)
                        print(f"Restored previous thread: {thread_id}")
                        print(f"Previous message count: {message_count}\n")
                    except Exception as e:
                        print(f"Could not load previous thread: {e}")
                        print("Creating new thread...\n")
                        thread = None
                
                # Create new thread if not loaded
                if thread is None:
                    print("Creating new thread...")
                    thread = await agents_client.create_thread()
                    print(f"New thread created: {thread.id}\n")
                
                print("="*70)
                print("Interactive Chat with Thread Persistence")
                print("="*70)
                print("Type 'quit' to exit")
                print("="*70 + "\n")
                
                while True:
                    try:
                        user_input = input("You: ").strip()
                    except (EOFError, KeyboardInterrupt):
                        print("\nExiting...")
                        break
                    
                    if user_input.lower() in ['quit', 'exit', 'q']:
                        print("\nDemo completed.")
                        print(f"Total messages: {message_count}")
                        break
                    
                    if not user_input:
                        continue
                    
                    message_count += 1
                    print(f"\n[Message #{message_count}]")
                    
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
                    
                    # Save thread info to file
                    print(f"[Saving thread state to {THREAD_FILE}...]")
                    thread_data = {
                        'timestamp': datetime.now().isoformat(),
                        'thread_id': thread.id,
                        'agent_id': agent.id,
                        'message_count': message_count
                    }
                    
                    with open(THREAD_FILE, 'w', encoding='utf-8') as f:
                        json.dump(thread_data, f, indent=2)
                    
                    print(f"Saved to {THREAD_FILE}\n")
                    print("-" * 70 + "\n")
                
                # Clean up
                print("\nCleaning up...")
                await agents_client.delete_agent(agent.id)
                print("Agent deleted.")
                
                print("\n" + "="*70)
                print("DEMO COMPLETE")
                print("="*70)
                print("Thread information saved to:", THREAD_FILE)
                print("The thread persists in Azure and can be reused.")
                print("="*70 + "\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
