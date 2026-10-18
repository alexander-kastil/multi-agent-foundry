import os
from dotenv import load_dotenv
from typing import Any
from pathlib import Path
from azure.identity import DefaultAzureCredential
from azure.ai.agents import AgentsClient
from azure.ai.agents.models import FunctionTool, ToolSet
from function_calling_functions import user_functions

def main(): 

    # Clear the console
    os.system('cls' if os.name=='nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    # Support both legacy and current variable naming
    model = os.getenv("MODEL_DEPLOYMENT") or os.getenv("MODEL_DEPLOYMENT_NAME")

    print(f"Agent Service: endpoint={endpoint}")
    print(f"Model deployment: {model}")

    if not endpoint or not model:
        print("ERROR: Missing required environment variables. Ensure PROJECT_ENDPOINT and MODEL_DEPLOYMENT_NAME are set in .env")
        return 1


    # Connect to the Azure AI Agents service
    agents_client = AgentsClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential()
    )


    # Define an agent that can use the custom functions
    with agents_client:

        functions = FunctionTool(user_functions)
        toolset = ToolSet()
        toolset.add(functions)
        
        # Enable automatic function calling
        agents_client.enable_auto_function_calls(toolset)
                
        try:
            agent = agents_client.create_agent(
                model=model,
                name="support-agent",
                instructions="""You are a technical support agent.
                                When a user has a technical issue, you get their email address and a description of the issue.
                                Then you use those values to submit a support ticket using the function available to you.
                                If a file is saved, tell the user the file name.
                            """,
                description="Demonstrates custom Function Calling with user-defined Python functions to create support tickets and save data to files.",
                toolset=toolset
            )
        except Exception as e:
            print(f"ERROR creating agent: {e}")
            return 1

        thread = agents_client.threads.create()
        print(f"You're chatting with: {agent.name} ({agent.id})")
    
        # Optional auto test mode for non-interactive runs
        auto_test_prompt = os.getenv("AUTO_TEST_PROMPT")
        if auto_test_prompt:
            try:
                agents_client.messages.create(
                    thread_id=thread.id,
                    role="user",
                    content=auto_test_prompt
                )
                run = agents_client.runs.create_and_process(thread_id=thread.id, agent_id=agent.id)
            except Exception as e:
                print(f"ERROR during run: {e}")
                return 1
            if run.status == "failed":
                print(f"Run failed: {run.last_error}")
            messages = agents_client.messages.list(thread_id=thread.id)
            # Get the last assistant message
            for msg in messages:
                if msg.role == "assistant" and msg.content:
                    for content in msg.content:
                        if hasattr(content, 'text') and hasattr(content.text, 'value'):
                            print(f"Assistant: {content.text.value}")
                            break
                    break
        else:
            # Enhanced interactive loop with graceful Ctrl+C exit
            input_prompt = (
                "Enter a prompt (type 'quit' to exit).\n"
                "Examples:\n"
                " - My email is alice@contoso.com and I cannot access the portal.\n"
                " - user@contoso.com: VS Code extension crashes on startup.\n"
                "Describe the technical issue (include an email) so I can file a ticket.\n> "
            )
            try:
                while True:
                    user_prompt = input(input_prompt)
                    if user_prompt.lower() == "quit":
                        print("Exiting on user request...")
                        break
                    if len(user_prompt.strip()) == 0:
                        print("Please enter a non-empty prompt.")
                        continue

                    try:
                        agents_client.messages.create(
                            thread_id=thread.id,
                            role="user",
                            content=user_prompt
                        )
                        run = agents_client.runs.create_and_process(thread_id=thread.id, agent_id=agent.id)
                    except Exception as e:
                        print(f"ERROR during run: {e}")
                        break
                    if run.status == "failed":
                        print(f"Run failed: {run.last_error}")

                    messages = agents_client.messages.list(thread_id=thread.id)
                    # Get the last assistant message
                    for msg in messages:
                        if msg.role == "assistant" and msg.content:
                            for content in msg.content:
                                if hasattr(content, 'text') and hasattr(content.text, 'value'):
                                    print(f"Assistant: {content.text.value}")
                                    break
                            break
            except KeyboardInterrupt:
                print("\nCtrl+C detected. Exiting gracefully...")


        # Get the conversation history
        print("\nConversation Log:\n")
        messages = agents_client.messages.list(thread_id=thread.id)
        message_list = list(messages)
        for message_data in reversed(message_list):
            if message_data.content:
                for content in message_data.content:
                    if hasattr(content, 'text') and hasattr(content.text, 'value'):
                        print(f"{message_data.role}: {content.text.value}\n")
                        break

        # Clean up
        delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
        if delete_on_exit:
            agents_client.delete_agent(agent.id)
            print("Deleted agent")
        else:
            print(f"Agent {agent.id} preserved for examination in Azure AI Foundry")
        agents_client.threads.delete(thread.id)
    

if __name__ == '__main__': 
    main()