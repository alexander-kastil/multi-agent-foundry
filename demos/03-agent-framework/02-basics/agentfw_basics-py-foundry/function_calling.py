"""
Function calling demo using Azure AI Foundry SDK.

This demo shows how to use Azure AI Agents with function calling (tools).
It demonstrates timing and function logging similar to middleware patterns.
"""

import asyncio
import os
from datetime import datetime
from dotenv import load_dotenv

from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.agents.aio import AgentsClient
from azure.ai.agents.models import FunctionTool

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("PROJECT_ENDPOINT")
MODEL_DEPLOYMENT = os.getenv("MODEL_DEPLOYMENT")


# Define tool functions
def get_weather(city: str) -> str:
    """Get current weather for a city."""
    weather_data = {
        "seattle": "Cloudy, 15°C, Light drizzle",
        "london": "Rainy, 12°C, Heavy rain",
        "tokyo": "Sunny, 22°C, Clear skies",
        "mumbai": "Partly cloudy, 28°C, Humid",
        "paris": "Partly cloudy, 18°C, Mild",
        "new york": "Snowy, -2°C, Light snow",
    }
    return weather_data.get(city.lower(), f"Weather data not available for {city}")


def calculate(expression: str) -> str:
    """Calculate a mathematical expression safely."""
    import ast
    import operator
    
    # Supported operators for safe evaluation
    ops = {
        ast.Add: operator.add,
        ast.Sub: operator.sub,
        ast.Mult: operator.mul,
        ast.Div: operator.truediv,
        ast.Pow: operator.pow,
        ast.USub: operator.neg,
    }
    
    def eval_expr(node):
        if isinstance(node, ast.Constant):
            return node.value
        elif isinstance(node, ast.BinOp):
            return ops[type(node.op)](eval_expr(node.left), eval_expr(node.right))
        elif isinstance(node, ast.UnaryOp):
            return ops[type(node.op)](eval_expr(node.operand))
        else:
            raise ValueError(f"Unsupported operation: {type(node)}")
    
    try:
        tree = ast.parse(expression, mode='eval')
        result = eval_expr(tree.body)
        return f"Result: {result}"
    except Exception as e:
        return f"Calculation error: {str(e)}"


def get_time() -> str:
    """Get the current time."""
    return f"Current time: {datetime.now().strftime('%I:%M:%S %p')}"


async def main():
    """Interactive demo with function calling."""
    
    print("\n" + "="*75)
    print("DEMO: Function Calling (Azure AI Foundry SDK)")
    print("="*75)
    print("""
This demo shows function calling with Azure AI Agents:

Available Functions:
1. get_weather(city) → Get weather for a city
2. calculate(expression) → Evaluate math expressions  
3. get_time() → Get current time

Watch how the agent calls these functions automatically!
""")
    print("="*75)
    
    async with DefaultAzureCredential() as credential:
        async with AIProjectClient(
            endpoint=PROJECT_ENDPOINT,
            credential=credential
        ) as project_client:
            
            async with AgentsClient(
                endpoint=PROJECT_ENDPOINT,
                credential=credential
            ) as agents_client:
                
                print("\nCreating agent with function calling tools...")
                
                # Define tools for the agent
                functions = {
                    "get_weather": get_weather,
                    "calculate": calculate,
                    "get_time": get_time
                }
                
                # Create function tool definitions
                tools = [
                    {
                        "type": "function",
                        "function": {
                            "name": "get_weather",
                            "description": "Get current weather for a city",
                            "parameters": {
                                "type": "object",
                                "properties": {
                                    "city": {
                                        "type": "string",
                                        "description": "The city name"
                                    }
                                },
                                "required": ["city"]
                            }
                        }
                    },
                    {
                        "type": "function",
                        "function": {
                            "name": "calculate",
                            "description": "Calculate a mathematical expression",
                            "parameters": {
                                "type": "object",
                                "properties": {
                                    "expression": {
                                        "type": "string",
                                        "description": "The math expression to evaluate"
                                    }
                                },
                                "required": ["expression"]
                            }
                        }
                    },
                    {
                        "type": "function",
                        "function": {
                            "name": "get_time",
                            "description": "Get the current time",
                            "parameters": {
                                "type": "object",
                                "properties": {}
                            }
                        }
                    }
                ]
                
                agent = await agents_client.create_agent(
                    model=MODEL_DEPLOYMENT,
                    name="Function Demo Agent",
                    instructions="You are a helpful assistant with access to tools. Use them when appropriate.",
                    tools=tools
                )
                
                print(f"Agent created: {agent.id}")
                
                # Create thread
                thread = await agents_client.create_thread()
                print(f"Thread created: {thread.id}")
                
                print("\n" + "="*75)
                print("SUGGESTED TEST PROMPTS:")
                print("="*75)
                print("""
1. "what's the weather in Tokyo?"
2. "what time is it and calculate 15 * 8"
3. "get weather in Paris"

Type 'quit' to exit
""")
                print("="*75 + "\n")
                
                while True:
                    try:
                        user_input = input("You: ").strip()
                        if not user_input:
                            continue
                        if user_input.lower() in ['quit', 'exit', 'bye']:
                            print("\nDemo ended.")
                            break
                        
                        start_time = datetime.now()
                        print(f"\n[TIMING] Started at {start_time.strftime('%H:%M:%S')}")
                        
                        # Create message
                        await agents_client.create_message(
                            thread_id=thread.id,
                            role="user",
                            content=user_input
                        )
                        
                        # Create run with function calling
                        run = await agents_client.create_run(
                            thread_id=thread.id,
                            assistant_id=agent.id
                        )
                        
                        # Poll for completion and handle function calls
                        while run.status in ["queued", "in_progress", "requires_action"]:
                            await asyncio.sleep(0.5)
                            run = await agents_client.get_run(thread_id=thread.id, run_id=run.id)
                            
                            if run.status == "requires_action":
                                tool_calls = run.required_action.submit_tool_outputs.tool_calls
                                tool_outputs = []
                                
                                for tool_call in tool_calls:
                                    function_name = tool_call.function.name
                                    import json
                                    function_args = json.loads(tool_call.function.arguments)
                                    
                                    print(f"\n[FUNCTION] Calling: {function_name}")
                                    print(f"[FUNCTION] Arguments: {function_args}")
                                    
                                    # Execute the function
                                    func = functions.get(function_name)
                                    if func:
                                        result = func(**function_args) if function_args else func()
                                        print(f"[FUNCTION] Result: {result}")
                                        
                                        tool_outputs.append({
                                            "tool_call_id": tool_call.id,
                                            "output": result
                                        })
                                
                                # Submit tool outputs
                                run = await agents_client.submit_tool_outputs(
                                    thread_id=thread.id,
                                    run_id=run.id,
                                    tool_outputs=tool_outputs
                                )
                        
                        # Get messages
                        messages = await agents_client.list_messages(thread_id=thread.id)
                        
                        # Display assistant response
                        print("\nAgent: ", end="")
                        for msg in messages.data:
                            if msg.role == "assistant" and msg.run_id == run.id:
                                for content in msg.content:
                                    if hasattr(content, 'text'):
                                        print(content.text.value)
                                break
                        
                        end_time = datetime.now()
                        duration = (end_time - start_time).total_seconds()
                        print(f"\n[TIMING] Completed in {duration:.2f} seconds\n")
                        
                    except (KeyboardInterrupt, EOFError):
                        print("\n\nSee you again soon.")
                        break
                    except Exception as e:
                        print(f"\nError: {e}\n")
                
                # Clean up
                print("\nCleaning up...")
                await agents_client.delete_agent(agent.id)
                print("Agent deleted.")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
