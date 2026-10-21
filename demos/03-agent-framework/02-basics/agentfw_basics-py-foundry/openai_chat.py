"""
Direct Azure OpenAI chat using Azure AI Foundry SDK.

This demo shows how to use azure-ai-inference directly for chat completions
without using the Agent Service. This is a non-persistent chat implementation.
"""

import os
from dotenv import load_dotenv
from azure.ai.inference import ChatCompletionsClient
from azure.ai.inference.models import SystemMessage, UserMessage
from azure.core.credentials import AzureKeyCredential

# Load environment variables
load_dotenv()

ENDPOINT = os.getenv("AZURE_OPENAI_ENDPOINT")
DEPLOYMENT = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")
API_KEY = os.getenv("AZURE_OPENAI_API_KEY")


def main():
    """Interactive demo: Azure OpenAI direct chat."""
    
    print("\n" + "="*70)
    print("DEMO: Direct Azure OpenAI Chat (Azure AI Foundry SDK)")
    print("="*70)
    
    # Create chat client
    client = ChatCompletionsClient(
        endpoint=ENDPOINT,
        credential=AzureKeyCredential(API_KEY)
    )
    
    # Initialize conversation with system message
    messages = [
        SystemMessage(content="You are a helpful assistant. Be concise and clear.")
    ]
    
    print("\nChat client created (temporary, not saved to cloud)")
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
        
        # Add user message
        messages.append(UserMessage(content=user_input))
        
        # Get streaming response
        print("Agent: ", end="", flush=True)
        
        response = client.complete(
            model=DEPLOYMENT,
            messages=messages,
            stream=True
        )
        
        assistant_message = ""
        for chunk in response:
            if chunk.choices and chunk.choices[0].delta.content:
                content = chunk.choices[0].delta.content
                print(content, end="", flush=True)
                assistant_message += content
        
        print("\n")
        
        # Add assistant response to conversation history
        from azure.ai.inference.models import AssistantMessage
        messages.append(AssistantMessage(content=assistant_message))


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\nSee you again soon.")
