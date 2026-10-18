import json
import os
import io
import sys

from azure.ai.agents.models import OpenApiAnonymousAuthDetails, OpenApiTool
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


def main() -> None:
    try:
        os.system("cls" if os.name == "nt" else "clear")
        load_dotenv()

        endpoint = os.getenv("PROJECT_ENDPOINT")
        model = os.getenv("MODEL_DEPLOYMENT")
        rest_url = os.getenv("REST_URL", "https://dummyjson.com")

        log(f"Using endpoint: {endpoint}")
        log(f"Using model: {model}")
        log(f"REST API URL: {rest_url}")

        # Define the OpenAPI specification for the DummyJSON Todos API
        openapi_spec = {
            "openapi": "3.0.4",
            "info": {
                "title": "DummyJSON-Todos",
                "version": "1.0.0",
                "description": "A simple REST API for managing todos"
            },
            "servers": [
                {
                    "url": rest_url
                }
            ],
            "paths": {
                "/todos": {
                    "get": {
                        "operationId": "getAllTodos",
                        "tags": ["Todos"],
                        "summary": "Get all todos",
                        "description": "Get all todos with optional limit and skip for pagination",
                        "parameters": [
                            {
                                "name": "limit",
                                "in": "query",
                                "schema": {"type": "integer", "default": 30},
                                "description": "Number of todos to return"
                            },
                            {
                                "name": "skip",
                                "in": "query",
                                "schema": {"type": "integer", "default": 0},
                                "description": "Number of todos to skip"
                            }
                        ],
                        "responses": {
                            "200": {
                                "description": "OK",
                                "content": {
                                    "application/json": {
                                        "schema": {
                                            "type": "object",
                                            "properties": {
                                                "todos": {
                                                    "type": "array",
                                                    "items": {"$ref": "#/components/schemas/Todo"}
                                                },
                                                "total": {"type": "integer"},
                                                "skip": {"type": "integer"},
                                                "limit": {"type": "integer"}
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                "/todos/add": {
                    "post": {
                        "operationId": "addTodo",
                        "tags": ["Todos"],
                        "summary": "Add a new todo",
                        "description": "Add a new todo (simulated - not persisted)",
                        "requestBody": {
                            "required": True,
                            "content": {
                                "application/json": {
                                    "schema": {"$ref": "#/components/schemas/TodoInput"}
                                }
                            }
                        },
                        "responses": {
                            "200": {
                                "description": "OK",
                                "content": {
                                    "application/json": {
                                        "schema": {"$ref": "#/components/schemas/Todo"}
                                    }
                                }
                            }
                        }
                    }
                }
            },
            "components": {
                "schemas": {
                    "TodoInput": {
                        "type": "object",
                        "properties": {
                            "todo": {"type": "string"},
                            "completed": {"type": "boolean"},
                            "userId": {"type": "integer"}
                        },
                        "required": ["todo", "completed", "userId"],
                        "additionalProperties": False
                    },
                    "Todo": {
                        "type": "object",
                        "properties": {
                            "id": {"type": "integer"},
                            "todo": {"type": "string"},
                            "completed": {"type": "boolean"},
                            "userId": {"type": "integer"}
                        },
                        "additionalProperties": False
                    }
                }
            }
        }

        log("Created OpenAPI specification for DummyJSON Todos API")

        agents_client = AgentsClient(
            endpoint=endpoint,
            credential=DefaultAzureCredential(),
        )
        log("Created AgentsClient")

        # Create OpenAPI tool with anonymous authentication
        auth = OpenApiAnonymousAuthDetails()
        openapi_tool = OpenApiTool(
            name="todos_api",
            spec=openapi_spec,
            description="Access the DummyJSON Todos API to manage todo items",
            auth=auth
        )
        log("Created OpenApiTool with anonymous authentication")

        with agents_client:
            agent = agents_client.create_agent(
                model=model,
                name="todos-agent",
                instructions=(
                    "You are a helpful assistant that helps manage todo items. "
                    "You can retrieve todos and add new todos to the list. "
                    "When adding todos, use realistic task descriptions. "
                    "Always confirm the results of operations to the user."
                ),
                description="Demonstrates calling REST APIs using OpenAPI 3.0 specification for todo management using DummyJSON.",
                tools=openapi_tool.definitions
            )
            log(f"Created agent: {agent.name} ({agent.id})")

            thread = agents_client.threads.create()
            log(f"Created thread: {thread.id}")

            print(f"\n{'='*70}")
            print(f"📋 Todos Management Agent (DummyJSON API Demo)")
            print(f"Agent: {agent.name} ({agent.id})")
            print(f"{'='*70}\n")

            # Step 1: Get initial todos
            print(f"📋 Step 1: Getting first 5 todos...")
            print(f"{'─'*70}\n")
            
            input("Press Enter to continue...")
            print()

            message = agents_client.messages.create(
                thread_id=thread.id,
                role="user",
                content="Please get the first 5 todos from the list"
            )
            log(f"Created message: {message['id'] if isinstance(message, dict) else message.id}")
            
            log("Creating and processing run...")
            run = agents_client.runs.create_and_process(
                thread_id=thread.id,
                agent_id=agent.id
            )
            log(f"Run completed with status: {run.status}")

            if run.status == "failed":
                print(f"\n❌ Error: {run.last_error}\n")
                log(f"Run error details: {run.last_error}")
            else:
                messages = agents_client.messages.list(thread_id=thread.id)
                log(f"Retrieved messages from thread")
                
                for message in messages:
                    if message.role == "assistant":
                        if isinstance(message.content, list) and len(message.content) > 0:
                            if message.content[0].get('type') == 'text':
                                assistant_text = message.content[0]['text']['value']
                                log(f"Assistant response: {assistant_text}")
                                print(f"\n{'─'*70}")
                                print(f"✅ Result:")
                                print(f"{assistant_text}")
                                print(f"{'─'*70}\n")
                        else:
                            log(f"Assistant message: {message.content}")
                            print(f"\n✅ Result: {message.content}\n")
                        break

            # Step 2: Add a new todo
            print(f"\n📋 Step 2: Adding a new todo item...")
            print(f"{'─'*70}\n")
            
            input("Press Enter to continue...")
            print()

            message = agents_client.messages.create(
                thread_id=thread.id,
                role="user",
                content="Please add a new todo: 'Test Azure AI Agents with DummyJSON API', not completed, for user ID 1"
            )
            log(f"Created message: {message['id'] if isinstance(message, dict) else message.id}")
            
            log("Creating and processing run...")
            run = agents_client.runs.create_and_process(
                thread_id=thread.id,
                agent_id=agent.id
            )
            log(f"Run completed with status: {run.status}")

            if run.status == "failed":
                print(f"\n❌ Error: {run.last_error}\n")
                log(f"Run error details: {run.last_error}")
            else:
                messages = agents_client.messages.list(thread_id=thread.id)
                log(f"Retrieved messages from thread")
                
                for message in messages:
                    if message.role == "assistant":
                        if isinstance(message.content, list) and len(message.content) > 0:
                            if message.content[0].get('type') == 'text':
                                assistant_text = message.content[0]['text']['value']
                                log(f"Assistant response: {assistant_text}")
                                print(f"\n{'─'*70}")
                                print(f"✅ Result:")
                                print(f"{assistant_text}")
                                print(f"{'─'*70}\n")
                        else:
                            log(f"Assistant message: {message.content}")
                            print(f"\n✅ Result: {message.content}\n")
                        break

            # Step 3: Verify the todo was added (get todos again)
            print(f"\n📋 Step 3: Verifying the new todo...")
            print(f"{'─'*70}\n")
            
            input("Press Enter to continue...")
            print()

            message = agents_client.messages.create(
                thread_id=thread.id,
                role="user",
                content="Can you get the first 3 todos again to show what we have?"
            )
            log(f"Created message: {message['id'] if isinstance(message, dict) else message.id}")
            
            log("Creating and processing run...")
            run = agents_client.runs.create_and_process(
                thread_id=thread.id,
                agent_id=agent.id
            )
            log(f"Run completed with status: {run.status}")

            if run.status == "failed":
                print(f"\n❌ Error: {run.last_error}\n")
                log(f"Run error details: {run.last_error}")
            else:
                messages = agents_client.messages.list(thread_id=thread.id)
                log(f"Retrieved messages from thread")
                
                for message in messages:
                    if message.role == "assistant":
                        if isinstance(message.content, list) and len(message.content) > 0:
                            if message.content[0].get('type') == 'text':
                                assistant_text = message.content[0]['text']['value']
                                log(f"Assistant response: {assistant_text}")
                                print(f"\n{'─'*70}")
                                print(f"✅ Result:")
                                print(f"{assistant_text}")
                                print(f"{'─'*70}\n")
                        else:
                            log(f"Assistant message: {message.content}")
                            print(f"\n✅ Result: {message.content}\n")
                        break

            # Clean-up
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
