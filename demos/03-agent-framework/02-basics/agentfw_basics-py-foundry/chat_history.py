"""
Chat history management demo using Azure AI Foundry SDK.

This demo shows how to manage conversation history by retrieving
messages from a thread and implementing simple message limiting.
"""

import asyncio
import os
import json
from pathlib import Path
from dotenv import load_dotenv

from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.agents.aio import AgentsClient

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("PROJECT_ENDPOINT")
MODEL_DEPLOYMENT = os.getenv("MODEL_DEPLOYMENT")

# Chat history file path
HISTORY_FILE = os.path.join("output", "chat_history.json")

# Maximum messages to keep in thread
MAX_MESSAGES = 20


async def main():
    """Interactive demo: Chat history management."""
    
    print("\n" + "="*70)
    print("DEMO: Chat History Management (Azure AI Foundry SDK)")
    print("="*70)
    
    # Ensure output directory exists
    os.makedirs("output", exist_ok=True)
    
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
                    name="ChatHistoryAgent",
                    instructions="You are a friendly assistant. Answer clearly and concisely.",
                    description="Agent with chat history management"
                )
                
                print(f"Agent created: {agent.id}")
                
                # Try to load existing thread
                thread = None
                if Path(HISTORY_FILE).exists():
                    try:
                        print(f"\nFound existing history: {HISTORY_FILE}")
                        load_choice = input("Load conversation history? (y/n): ").strip().lower()
                        if load_choice in ['y', 'yes']:
                            with open(HISTORY_FILE, 'r', encoding='utf-8') as f:
                                history_data = json.load(f)
                            
                            thread_id = history_data.get('thread_id')
                            thread = await agents_client.get_thread(thread_id)
                            
                            # List messages in thread
                            messages = await agents_client.list_messages(thread_id=thread.id)
                            print(f"Loaded thread with {len(messages.data)} messages.")
                    except Exception as e:
                        print(f"Could not load history: {e}")
                        print("Starting with new thread.")
                
                # Create new thread if not loaded
                if thread is None:
                    print("\nCreating new thread...")
                    thread = await agents_client.create_thread()
                    print(f"Thread created: {thread.id}")
                
                print("\nType 'quit' to exit.\n")
                
                while True:
                    try:
                        user_input = input("You: ")
                    except (EOFError, KeyboardInterrupt):
                        print("\nExiting chat demo.")
                        break
                    
                    if not user_input or user_input.strip().lower() in {"quit", "exit", "q"}:
                        print("\nGoodbye!")
                        break
                    
                    # Create message in thread
                    await agents_client.create_message(
                        thread_id=thread.id,
                        role="user",
                        content=user_input
                    )
                    
                    # Create and process run
                    print("Assistant: ", end="", flush=True)
                    
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
                    
                    # Get and display message count
                    messages = await agents_client.list_messages(thread_id=thread.id)
                    message_list = messages.data
                    
                    print(f"- Number of messages in thread: {len(message_list)}")
                    
                    # Show recent messages (up to 5)
                    for idx, msg in enumerate(reversed(message_list[:5]), start=1):
                        role = msg.role
                        content = ""
                        if hasattr(msg, 'content') and msg.content:
                            for c in msg.content:
                                if hasattr(c, 'text') and hasattr(c.text, 'value'):
                                    content = c.text.value
                                    break
                        
                        text_preview = (content[:100] + "...") if len(content) > 100 else content
                        print(f"  {idx}. {role}: {text_preview}")
                    
                    # Save thread info
                    history_data = {
                        'thread_id': thread.id,
                        'agent_id': agent.id,
                        'message_count': len(message_list)
                    }
                    with open(HISTORY_FILE, 'w', encoding='utf-8') as f:
                        json.dump(history_data, f, indent=2)
                    
                    print("\n" + "-" * 60 + "\n")
                
                # Clean up
                print("\nCleaning up...")
                await agents_client.delete_agent(agent.id)
                print("Agent deleted.")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nDemo interrupted.")
    except Exception as e:
        print(f"\nError: {e}")
