"""
Multi-turn Interactive Chat with Azure AI Model Router

This demo demonstrates:
- Interactive console-based chat session with conversation history
- Using Azure AI Foundry's model router to automatically select the best model for each turn
- Maintaining context across multiple turns with a rolling history window
- Displaying which underlying model was selected by the router for each response
- Configurable parameters (temperature, max tokens, history size) via environment variables

The model router intelligently selects from available models based on the request characteristics,
and the response.model property reveals which specific model handled each turn.
"""

import os
from typing import List, Dict
from azure.ai.projects import AIProjectClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv


def main():
    load_dotenv()

    # Load environment variables
    project_endpoint = os.environ["PROJECT_ENDPOINT"]
    router_model = os.environ["MODEL_ROUTER"]
    
    # System prompt can be customized via env
    system_prompt = os.getenv("SYSTEM_PROMPT", "You are a helpful assistant.")
    max_history_turns = int(os.getenv("MAX_HISTORY_TURNS", "12"))
    max_output_tokens = int(os.getenv("MAX_OUTPUT_TOKENS", "10000"))
    temperature = float(os.getenv("TEMPERATURE", "0.7"))
    top_p = float(os.getenv("TOP_P", "0.95"))
    frequency_penalty = float(os.getenv("FREQ_PENALTY", "0.0"))
    presence_penalty = float(os.getenv("PRES_PENALTY", "0.0"))

    project_client = AIProjectClient(
        endpoint=project_endpoint,
        credential=DefaultAzureCredential()
    )

    client = project_client.get_openai_client()

    # Conversation state: always begin with system message
    messages: List[Dict[str, str]] = [
        {"role": "system", "content": system_prompt},
    ]

    print("Interactive chat started. Type 'exit' or 'quit' to stop.\n")

    while True:
        try:
            user_input = input("You: ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\nExiting.")
            break

        if not user_input:
            continue
        if user_input.lower() in {"exit", "quit"}:
            print("Goodbye.")
            break

        # Append user turn
        messages.append({"role": "user", "content": user_input})

        # Trim history to keep context small while preserving the system message
        trimmed = trim_history(messages, max_history_turns=max_history_turns)

        # Call the router deployment
        response = client.chat.completions.create(
            messages=trimmed,
            max_tokens=max_output_tokens,
            temperature=temperature,
            top_p=top_p,
            frequency_penalty=frequency_penalty,
            presence_penalty=presence_penalty,
            model=router_model,
        )

        assistant_message = response.choices[0].message.content or ""
        routed_model = getattr(response, "model", "<unknown>")

        print("=" * 50)
        print(f"Model chosen by the router: {routed_model}")
        print("=" * 50)
        print(f"Assistant: {assistant_message}\n")

        # Append assistant turn to the full history
        messages.append({"role": "assistant", "content": assistant_message})


def trim_history(messages: List[Dict[str, str]], max_history_turns: int) -> List[Dict[str, str]]:
    """Keep system message plus the last N user/assistant turns.

    messages: full history including system at index 0
    max_history_turns: number of recent user+assistant pairs to retain
    """
    if not messages:
        return messages

    # Always preserve the first message if it is the system message
    head = messages[0:1]
    body = messages[1:]

    # Count pairs in body. We'll keep the last 2*max_history_turns messages
    keep = max_history_turns * 2
    if len(body) <= keep:
        return head + body
    else:
        return head + body[-keep:]


if __name__ == "__main__":
    main()