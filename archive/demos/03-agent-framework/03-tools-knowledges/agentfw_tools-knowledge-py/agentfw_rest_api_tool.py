"""
REST API Tool Demo - DummyJSON Todos API

This demo shows how to create custom tools that call external REST APIs.
We'll use the DummyJSON Todos API to demonstrate querying and managing todo items.

API: https://dummyjson.com/docs/todos
"""

import asyncio
import os
from typing import Annotated, Optional
from pydantic import Field
from dotenv import load_dotenv
import requests

from agent_framework.azure import AzureOpenAIChatClient

# Load environment variables
load_dotenv()

ENDPOINT = os.getenv("AZURE_OPENAI_ENDPOINT")
DEPLOYMENT = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")
API_KEY = os.getenv("AZURE_OPENAI_API_KEY")
API_VERSION = os.getenv("AZURE_OPENAI_API_VERSION", "2024-07-01-preview")

# DummyJSON API base URL from environment
REST_API_BASE = os.getenv("REST_URL", "https://dummyjson.com")


# Define REST API tools
def get_todos(
    limit: Annotated[Optional[int], Field(description="Number of todos to retrieve (default 10)")] = 10,
    skip: Annotated[Optional[int], Field(description="Number of todos to skip for pagination")] = 0
) -> str:
    """Get all todos with optional pagination."""
    try:
        params = {'limit': limit, 'skip': skip}
        
        response = requests.get(
            f"{REST_API_BASE}/todos",
            params=params,
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            todos = data.get('todos', [])
            total = data.get('total', 0)
            
            if not todos:
                return "No todos found."
            
            result = f"Todos (showing {len(todos)} of {total}):\n"
            for todo in todos:
                todo_id = todo.get('id', 'N/A')
                task = todo.get('todo', 'Unknown')
                completed = '✓' if todo.get('completed') else '✗'
                user_id = todo.get('userId', 'N/A')
                result += f"- [{completed}] #{todo_id}: {task} (User: {user_id})\n"
            
            return result
        else:
            return f"Error: API returned status {response.status_code}"
    except requests.exceptions.Timeout:
        return "Error: Request timed out. The API might be unavailable."
    except requests.exceptions.RequestException as e:
        return f"Error: Could not connect to DummyJSON API - {str(e)}"


def get_todo_by_id(
    todo_id: Annotated[int, Field(description="The ID of the todo item to get")]
) -> str:
    """Get a specific todo by its ID."""
    try:
        response = requests.get(
            f"{REST_API_BASE}/todos/{todo_id}",
            timeout=10
        )
        
        if response.status_code == 200:
            todo = response.json()
            completed = '✓ Completed' if todo.get('completed') else '✗ Not completed'
            result = f"Todo Details:\n"
            result += f"ID: {todo.get('id', 'N/A')}\n"
            result += f"Task: {todo.get('todo', 'Unknown')}\n"
            result += f"Status: {completed}\n"
            result += f"User ID: {todo.get('userId', 'N/A')}\n"
            return result
        elif response.status_code == 404:
            return f"Todo with ID {todo_id} not found."
        else:
            return f"Error: API returned status {response.status_code}"
    except requests.exceptions.Timeout:
        return "Error: Request timed out. The API might be unavailable."
    except requests.exceptions.RequestException as e:
        return f"Error: Could not connect to DummyJSON API - {str(e)}"


def get_todos_by_user(
    user_id: Annotated[int, Field(description="The user ID to get todos for")]
) -> str:
    """Get all todos for a specific user."""
    try:
        response = requests.get(
            f"{REST_API_BASE}/todos/user/{user_id}",
            timeout=10
        )
        
        if response.status_code == 200:
            data = response.json()
            todos = data.get('todos', [])
            total = data.get('total', 0)
            
            if not todos:
                return f"No todos found for user {user_id}."
            
            result = f"Todos for User {user_id} ({total} total):\n"
            for todo in todos:
                todo_id = todo.get('id', 'N/A')
                task = todo.get('todo', 'Unknown')
                completed = '✓' if todo.get('completed') else '✗'
                result += f"- [{completed}] #{todo_id}: {task}\n"
            return result
        else:
            return f"Error: API returned status {response.status_code}"
    except requests.exceptions.Timeout:
        return "Error: Request timed out. The API might be unavailable."
    except requests.exceptions.RequestException as e:
        return f"Error: Could not connect to DummyJSON API - {str(e)}"


async def main():
    """Interactive demo: Agent with REST API tools."""
    
    print("\n" + "="*70)
    print("✅ DEMO: REST API Tools - DummyJSON Todos")
    print("="*70)
    print(f"""
This demo shows how to create custom tools that call REST APIs.

API: {REST_API_BASE}/todos

Available Tools:
1. get_todos - Get all todos with pagination
2. get_todo_by_id - Get a specific todo by ID
3. get_todos_by_user - Get all todos for a specific user

Example queries:
- "Show me the first 5 todos"
- "Get todo with ID 10"
- "Show me all todos for user 5"
- "List todos from 10 to 20"
- "What's in todo number 1?"
    """)
    
    # Create agent with REST API tools
    agent = AzureOpenAIChatClient(
        endpoint=ENDPOINT,
        deployment_name=DEPLOYMENT,
        api_key=API_KEY,
        api_version=API_VERSION
    ).create_agent(
        instructions=(
            "You are a helpful assistant with access to a todos API. "
            "Use the tools to help users browse todos, get specific todo details, and find todos by user. "
            "Always provide clear and helpful responses. "
            "If the API is unavailable, inform the user politely and suggest trying again later."
        ),
        name="TodoBot",
        tools=[get_todos, get_todo_by_id, get_todos_by_user]
    )
    
    print("\n✅ Agent created with REST API tools")
    
    print("\n" + "="*70)
    print("💬 Interactive Chat (Type 'quit' to exit)")
    print("="*70 + "\n")
    
    while True:
        try:
            user_input = input("You: ")
        except EOFError:
            print("\n👋 Received EOF - exiting.")
            break
        except KeyboardInterrupt:
            print("\n👋 Interrupted - exiting.")
            break
        
        if user_input.lower() in ['quit', 'exit', 'q']:
            print("\n👋 Goodbye!")
            break
        
        if not user_input.strip():
            continue
        
        print("Agent: ", end="", flush=True)
        async for chunk in agent.run_stream(user_input):
            if chunk.text:
                print(chunk.text, end="", flush=True)
        print("\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n👋 See you again soon.")
