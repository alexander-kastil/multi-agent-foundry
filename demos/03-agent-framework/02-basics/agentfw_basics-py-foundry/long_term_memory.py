"""
Long-term memory demo using Azure AI Foundry SDK.

This demo shows how to implement AI-powered long-term memory
by using thread persistence and selective context management.
"""

import asyncio
import os
import json
from pathlib import Path
from datetime import datetime
from dotenv import load_dotenv

from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.agents.aio import AgentsClient

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("PROJECT_ENDPOINT")
MODEL_DEPLOYMENT = os.getenv("MODEL_DEPLOYMENT")

# Memory file
MEMORY_FILE = os.path.join("output", "long_term_memory.json")


async def extract_key_facts(agents_client, agent_id, conversation_summary):
    """Extract key facts from conversation using AI."""
    
    # Create a temporary thread for extraction
    temp_thread = await agents_client.create_thread()
    
    await agents_client.create_message(
        thread_id=temp_thread.id,
        role="user",
        content=f"Extract key facts and important information from this conversation summary:\n\n{conversation_summary}\n\nProvide a concise bullet-point list of key facts to remember."
    )
    
    # Run extraction
    run = await agents_client.create_run(
        thread_id=temp_thread.id,
        assistant_id=agent_id
    )
    
    # Wait for completion
    while run.status in ["queued", "in_progress"]:
        await asyncio.sleep(0.5)
        run = await agents_client.get_run(thread_id=temp_thread.id, run_id=run.id)
    
    # Get extracted facts
    messages = await agents_client.list_messages(thread_id=temp_thread.id)
    facts = ""
    for msg in messages.data:
        if msg.role == "assistant":
            for content in msg.content:
                if hasattr(content, 'text'):
                    facts = content.text.value
                    break
            break
    
    return facts


async def main():
    """Interactive demo with long-term memory."""
    
    print("\n" + "="*70)
    print("DEMO: Long-Term Memory (Azure AI Foundry SDK)")
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
                
                print("\nCreating memory-enabled agent...")
                
                agent = await agents_client.create_agent(
                    model=MODEL_DEPLOYMENT,
                    name="Memory Agent",
                    instructions=(
                        "You are a helpful assistant with long-term memory. "
                        "Remember important facts about the user and refer back to them. "
                        "Be personal and reference past conversations when relevant."
                    ),
                    description="Agent with long-term memory capabilities"
                )
                
                print(f"Agent created: {agent.id}")
                
                # Load or initialize memory
                memory_data = {"facts": [], "threads": []}
                
                if Path(MEMORY_FILE).exists():
                    try:
                        print(f"\nLoading long-term memory from {MEMORY_FILE}...")
                        with open(MEMORY_FILE, 'r', encoding='utf-8') as f:
                            memory_data = json.load(f)
                        
                        print(f"Loaded {len(memory_data.get('facts', []))} facts from memory")
                        if memory_data.get('facts'):
                            print("\nKnown facts:")
                            for fact in memory_data['facts'][:5]:
                                print(f"  - {fact}")
                    except Exception as e:
                        print(f"Could not load memory: {e}")
                
                # Create new thread
                thread = await agents_client.create_thread()
                print(f"\nNew conversation thread: {thread.id}")
                
                # If we have memory, inject it as context
                if memory_data.get('facts'):
                    context = "Here's what I remember about you:\n" + "\n".join(
                        f"- {fact}" for fact in memory_data['facts']
                    )
                    await agents_client.create_message(
                        thread_id=thread.id,
                        role="user",
                        content=f"[System: {context}]"
                    )
                
                print("\n" + "="*70)
                print("Interactive Chat with Long-Term Memory")
                print("="*70)
                print("Type 'save' to save current conversation to memory")
                print("Type 'quit' to exit")
                print("="*70 + "\n")
                
                messages_in_conversation = []
                
                while True:
                    try:
                        user_input = input("You: ").strip()
                        
                        if not user_input:
                            continue
                        
                        if user_input.lower() in ['quit', 'exit', 'q']:
                            print("\nGoodbye!")
                            break
                        
                        if user_input.lower() == 'save':
                            print("\n[Extracting key facts from conversation...]")
                            
                            # Get conversation summary
                            summary = "\n".join(messages_in_conversation)
                            
                            # Extract facts using AI
                            facts = await extract_key_facts(agents_client, agent.id, summary)
                            
                            # Save to memory
                            if facts:
                                memory_data['facts'].append(facts)
                                memory_data['threads'].append({
                                    'thread_id': thread.id,
                                    'timestamp': datetime.now().isoformat(),
                                    'facts': facts
                                })
                                
                                with open(MEMORY_FILE, 'w', encoding='utf-8') as f:
                                    json.dump(memory_data, f, indent=2)
                                
                                print(f"[Saved to long-term memory: {MEMORY_FILE}]")
                                print(f"\nExtracted facts:\n{facts}\n")
                            
                            continue
                        
                        # Normal conversation
                        messages_in_conversation.append(f"User: {user_input}")
                        
                        await agents_client.create_message(
                            thread_id=thread.id,
                            role="user",
                            content=user_input
                        )
                        
                        print("Agent: ", end="", flush=True)
                        
                        response_text = ""
                        async with await agents_client.create_run_stream(
                            thread_id=thread.id,
                            assistant_id=agent.id
                        ) as stream:
                            async for event in stream:
                                if event.event == "thread.message.delta":
                                    if hasattr(event.data, 'delta') and hasattr(event.data.delta, 'content'):
                                        for content in event.data.delta.content:
                                            if hasattr(content, 'text') and hasattr(content.text, 'value'):
                                                text = content.text.value
                                                print(text, end="", flush=True)
                                                response_text += text
                        
                        print("\n")
                        messages_in_conversation.append(f"Assistant: {response_text}")
                    
                    except (KeyboardInterrupt, EOFError):
                        print("\n\nExiting...")
                        break
                    except Exception as e:
                        print(f"\nError: {e}\n")
                
                # Clean up
                print("\nCleaning up...")
                await agents_client.delete_agent(agent.id)
                print("Agent deleted.")
                
                print("\n" + "="*70)
                print("DEMO COMPLETE")
                print("="*70)
                print(f"Long-term memory saved to: {MEMORY_FILE}")
                print("Memory persists across sessions")
                print("="*70 + "\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
