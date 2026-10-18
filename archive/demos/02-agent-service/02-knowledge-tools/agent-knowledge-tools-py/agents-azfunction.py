import json
import os
import io
import sys
from typing import Any, Dict, Optional

import requests
from azure.ai.agents.models import FunctionTool, ToolSet
from azure.ai.agents import AgentsClient
from azure.identity import DefaultAzureCredential
from dotenv import load_dotenv

# Configure UTF-8 encoding for Windows console (fixes emoji display issues)
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')


def log(message: str) -> None:
    """Print log message if detailed logging is enabled."""
    detailed_logging = os.getenv("DETAILED_LOGGING", "false").lower() == "true"
    if detailed_logging:
        print(f"[LOG] {message}")


def convert_currency_via_function(
    from_currency: str,
    to_currency: str,
    amount: float,
    date: Optional[str] = None,
) -> str:

    FUNCTION_DEPLOYMENT_URL = os.getenv("FUNCTION_DEPLOYMENT_URL")

    normalized_from = from_currency.upper()
    normalized_to = to_currency.upper()

    payload: Dict[str, Any] = {
        "from": normalized_from,
        "to": normalized_to,
        "amount": float(amount),
    }

    if date:
        payload["date"] = date

    response = requests.post(FUNCTION_DEPLOYMENT_URL.rstrip("/"), json=payload, timeout=20)
    response.raise_for_status()

    conversion = response.json()

    summary = (
        f"{payload['amount']} {normalized_from} equals "
        f"{conversion.get('result')} {normalized_to} on {conversion.get('date')}"
    )

    return json.dumps({
        "summary": summary,
        "details": conversion,
    })


def main() -> None:
    try:
        os.system("cls" if os.name == "nt" else "clear")
        load_dotenv()

        endpoint = os.getenv("PROJECT_ENDPOINT")
        model = os.getenv("MODEL_DEPLOYMENT")

        log(f"Using endpoint: {endpoint}")
        log(f"Using model: {model}")
        log(f"Azure Function endpoint: {os.getenv('FUNCTION_DEPLOYMENT_URL')}")

        agents_client = AgentsClient(
            endpoint=endpoint,
            credential=DefaultAzureCredential(),
        )
        log("Created AgentsClient")

        functions = FunctionTool([convert_currency_via_function])
        toolset = ToolSet()
        toolset.add(functions)
        log("Created ToolSet with FunctionTool")

        with agents_client:
            # Enable automatic function calling
            agents_client.enable_auto_function_calls(toolset)
            log("Enabled auto function calls")

            agent = agents_client.create_agent(
                model=model,
                name="currency-conversion-agent",
                instructions=(
                    "You assist with currency conversion questions by calling the convert_currency_via_function."
                    "Always call the tool when a user provides currencies or amounts."
                ),
                description="Demonstrates calling external Azure Functions as tools for real-time currency conversion using live exchange rates.",
                toolset=toolset
            )
            log(f"Created agent: {agent.name} ({agent.id})")

            thread = agents_client.threads.create()
            log(f"Created thread: {thread.id}")
            
            print(f"\n{'='*70}")
            print(f"💱 Currency Conversion Agent")
            print(f"Agent: {agent.name} ({agent.id})")
            print(f"{'='*70}\n")

            while True:
                try:
                    user_input = input("Enter currencies to exchange (e.g., '100 EUR to THB') or 'quit' to exit:\n> ").strip()
                    
                    if user_input.lower() == "quit":
                        break
                    
                    if not user_input:
                        print("Please enter a currency conversion request.\n")
                        continue

                    log(f"User input: {user_input}")

                    message = agents_client.messages.create(
                        thread_id=thread.id,
                        role="user",
                        content=user_input
                    )
                    log(f"Created message: {message['id'] if isinstance(message, dict) else message.id}")
                    
                    log("Creating and processing run...")
                    run = agents_client.runs.create_and_process(
                        thread_id=thread.id, 
                        agent_id=agent.id,
                        toolset=toolset
                    )
                    log(f"Run completed with status: {run.status}")

                    if run.status == "failed":
                        print(f"\n❌ Error: {run.last_error}\n")
                        log(f"Run error details: {run.last_error}")
                        continue

                    messages = agents_client.messages.list(thread_id=thread.id)
                    log(f"Retrieved messages from thread")
                    
                    for message in messages:
                        if message.role == "assistant":
                            # Extract text from message content
                            if isinstance(message.content, list) and len(message.content) > 0:
                                if message.content[0].get('type') == 'text':
                                    assistant_text = message.content[0]['text']['value']
                                    log(f"Assistant response: {assistant_text}")
                                    print(f"\n{'─'*70}")
                                    print(f"✅ Result:")
                                    print(f"   {assistant_text}")
                                    print(f"{'─'*70}\n")
                            else:
                                log(f"Assistant message: {message.content}")
                                print(f"\n✅ Result: {message.content}\n")
                            break
                    
                    # Ask if user wants to continue
                    while True:
                        continue_prompt = input("Do you want to exchange anything else? (y/n): ").strip().lower()
                        if continue_prompt in ['y', 'yes']:
                            print()
                            break
                        elif continue_prompt in ['n', 'no']:
                            print("\nThank you for using the Currency Conversion Agent!")
                            break
                        else:
                            print("Please enter 'y' or 'n'")
                    
                    if continue_prompt in ['n', 'no']:
                        break
                        
                except KeyboardInterrupt:
                    print("\n\n⚠️  Interrupted by user. Exiting gracefully...")
                    log("User interrupted with Ctrl+C")
                    break
                except Exception as e:
                    print(f"\n❌ Error: {type(e).__name__}: {e}\n")
                    log(f"Error during conversation: {type(e).__name__}: {e}")
                    continue

            delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
            if delete_on_exit:
                agents_client.delete_agent(agent.id)
                log(f"Deleted agent: {agent.id}")
                print(f"\n{'='*70}")
                print("🗑️  Agent deleted successfully")
                print(f"{'='*70}\n")
            else:
                log(f"Agent {agent.id} preserved for examination")
                print(f"\n{'='*70}")
                print(f"💾  Agent {agent.id} preserved for examination in Azure AI Foundry")
                print(f"{'='*70}\n")
            
    except KeyboardInterrupt:
        print("\n\n⚠️  Interrupted by user. Exiting gracefully...")
        log("User interrupted with Ctrl+C during startup")
    except Exception as e:
        print(f"\n❌ Error: {type(e).__name__}: {e}")
        if os.getenv("DETAILED_LOGGING", "false").lower() == "true":
            import traceback
            traceback.print_exc()


if __name__ == "__main__":
    main()
