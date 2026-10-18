"""
Chat history demo: demonstrates chat history management using reducers.

This file follows the style of the other demos in this folder. It shows two
reducers:
 - MessageCountingChatReducer: keeps only the last N messages
 - SummarizingChatReducer: when message count passes a threshold, summarizes
   older messages into a single summary message using the LLM and replaces them
   with the summary (reduces storage size while preserving context).

The demo runs an interactive loop similar to the provided C# sample: it accepts
user input, runs the agent, stores the exchange to an in-memory store, and
prints the stored messages count and contents.

Features:
 - Serialization: automatically saves chat history to JSON after each update
 - Deserialization: on startup, prompts to load previous conversation history
 - Reusable utilities: all chat history classes are in utils/chat_history.py

Notes:
 - This is a demo. The summarizer calls the same Azure OpenAI endpoint to produce
   a short summary. Ensure environment variables for Azure are set when running.
 - The agent created for chatting is separate from the summarizer agent.
"""

import asyncio
import os
from pathlib import Path
from dotenv import load_dotenv

from agent_framework.azure import AzureOpenAIChatClient
from utils.chat_history import (
    SimpleMessage,
    MessageCountingChatReducer,
    SummarizingChatReducer,
    InMemoryChatMessageStore
)

# Load environment variables
load_dotenv()

ENDPOINT = os.getenv("AZURE_OPENAI_ENDPOINT")
DEPLOYMENT = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")
API_KEY = os.getenv("AZURE_OPENAI_API_KEY")
API_VERSION = os.getenv("AZURE_OPENAI_API_VERSION", "2024-07-01-preview")

# Chat history file path
HISTORY_FILE = os.path.join("output", "chat_history.json")


# -- Demo: interactive chat with history management -------------------------
async def main():
    print("\n" + "=" * 70)
    print("DEMO: Chat History Management with Reducers")
    print("=" * 70)

    if not (ENDPOINT and DEPLOYMENT and API_KEY):
        print("\nMissing Azure OpenAI configuration. Please set AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_CHAT_DEPLOYMENT_NAME and AZURE_OPENAI_API_KEY in your environment or .env file.")
        return

    # Create primary chat agent
    chat_agent = AzureOpenAIChatClient(
        endpoint=ENDPOINT,
        deployment_name=DEPLOYMENT,
        api_key=API_KEY,
        api_version=API_VERSION,
    ).create_agent(
        instructions="You are a friendly assistant. Answer clearly and concisely.",
        name="ChatHistoryAgent",
    )

    # Create a lightweight summarizer agent (same endpoint but different instructions)
    summarizer_agent = AzureOpenAIChatClient(
        endpoint=ENDPOINT,
        deployment_name=DEPLOYMENT,
        api_key=API_KEY,
        api_version=API_VERSION,
    ).create_agent(
        instructions=(
            "You are a concise summarizer. Produce a short summary of a conversation. "
            "Return only plain text, no JSON or extra commentary."
        ),
        name="SummarizerAgent",
    )

    # Create reducers and message store
    msg_count_reducer = MessageCountingChatReducer(target_count=12)
    summarizing_reducer = SummarizingChatReducer(summarizer_agent, threshold=10, retain_last=4)

    # Chain reducers: summarizing first (reduce old messages), then counting as safeguard
    store = InMemoryChatMessageStore(
        reducers=[summarizing_reducer, msg_count_reducer],
        auto_save_path=HISTORY_FILE
    )

    print("\nAgents created and in-memory message store initialized.")
    
    # Prompt user to load previous conversation history
    if Path(HISTORY_FILE).exists():
        print(f"\nFound existing conversation history: {HISTORY_FILE}")
        try:
            load_choice = input("Do you want to load the conversation history? (y/n): ").strip().lower()
            if load_choice in ['y', 'yes']:
                store.load_from_file(HISTORY_FILE)
                messages_loaded = await store.get_messages()
                print(f"Loaded {len(messages_loaded)} message(s) from history.")
            else:
                print("Starting with empty history.")
        except (EOFError, KeyboardInterrupt):
            print("\nStarting with empty history.")
    else:
        print("\nNo previous history found. Starting fresh.")
    
    print("\nType 'quit' to exit.\n")

    # Interactive loop
    while True:
        try:
            user_input = input("You: ")
        except (EOFError, KeyboardInterrupt):
            print("\nExiting chat demo.")
            break

        if not user_input or user_input.strip().lower() in {"quit", "exit", "q"}:
            print("\nGoodbye!")
            break

        # Add user message to the store
        await store.add_message(SimpleMessage(role="user", text=user_input))

        # Prepare messages to send to the agent: convert store messages to the agent's expected format if needed
        # For simplicity, we'll send the concatenated recent history as a single prompt.
        history = await store.get_messages()
        prompt_parts = [f"{m.role.upper()}: {m.text}" for m in history]
        composed_prompt = "\n\n".join(prompt_parts) + "\n\nASSISTANT:"  # nudges model to reply

        # Run agent (use non-streaming run for simplicity)
        try:
            response = await chat_agent.run(composed_prompt)
            # Agent.run may return an object with .text or a string; handle both
            assistant_text = getattr(response, "text", None) or str(response)
            assistant_text = assistant_text.strip()

            # Print assistant reply
            print(f"Assistant: {assistant_text}\n")

            # Add assistant message to the store
            await store.add_message(SimpleMessage(role="assistant", text=assistant_text))

        except Exception as e:
            print(f"\nError calling agent: {e}")

        # Show current stored messages
        messages_in_store = await store.get_messages()
        print(f"- Number of messages in store: {len(messages_in_store)}")
        for idx, m in enumerate(messages_in_store, start=1):
            # Truncate long messages for display
            text_preview = (m.text[:200] + "...") if len(m.text) > 200 else m.text
            print(f"  {idx}. {m.role}: {text_preview}")

        print("\n" + "-" * 60 + "\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nDemo interrupted.")
    except Exception as e:
        print(f"\nError: {e}")
