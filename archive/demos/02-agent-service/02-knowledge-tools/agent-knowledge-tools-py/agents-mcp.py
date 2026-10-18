import os
import io
import sys
import time
from dotenv import load_dotenv
from azure.ai.agents import AgentsClient
from azure.identity import DefaultAzureCredential
from azure.ai.agents.models import (
    ListSortOrder,
    McpTool,
    MessageRole,
    RequiredMcpToolCall,
    SubmitToolApprovalAction,
    ToolApproval,
)

# Configure UTF-8 encoding for Windows console (fixes emoji display issues)
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

def main():
    # Clear the console to keep the output focused on the agent interaction
    os.system('cls' if os.name == 'nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    
    # Get MCP server configuration from environment variables
    mcp_server_url = os.getenv("MCP_SERVER_URL", "https://gitmcp.io/Azure/azure-rest-api-specs")
    mcp_server_label = os.getenv("MCP_SERVER_LABEL", "github")

    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"MCP Server: {mcp_server_label} at {mcp_server_url}")

    agents_client = AgentsClient(
        endpoint=endpoint,
        credential=DefaultAzureCredential(),
    )

    # [START create_agent_with_mcp_tool]
    # Initialize agent MCP tool
    mcp_tool = McpTool(
        server_label=mcp_server_label,
        server_url=mcp_server_url,
        allowed_tools=[],  # Optional: specify allowed tools
    )

    # You can also add or remove allowed tools dynamically
    search_api_code = "search_azure_rest_api_code"
    mcp_tool.allow_tool(search_api_code)
    print(f"Allowed tools: {mcp_tool.allowed_tools}")

    # Create agent with MCP tool and process agent run
    with agents_client:
        # Create a new agent.
        # NOTE: To reuse existing agent, fetch it with get_agent(agent_id)
        agent = agents_client.create_agent(
            model=model,
            name="mcp-agent",
            instructions="You are a helpful agent that can use MCP tools to assist users. Use the available MCP tools to answer questions and perform tasks.",
            description="Demonstrates Model Context Protocol (MCP) integration to access external tools and services (e.g., GitHub API specs) with approval workflows.",
            tools=mcp_tool.definitions,
        )
        # [END create_agent_with_mcp_tool]

        print(f"Created agent, agent ID: {agent.id}")

        # Create thread for communication
        thread = agents_client.threads.create()
        print(f"Created thread, thread ID: {thread.id}")

        # Create message to thread
        message = agents_client.messages.create(
            thread_id=thread.id,
            role="user",
            content="Please summarize the Azure REST API specifications Readme",
        )
        print(f"Created message, message ID: {message.id}")

        # [START handle_tool_approvals]
        # Create and process agent run in thread with MCP tools
        mcp_tool.update_headers("SuperSecret", "123456")
        # mcp_tool.set_approval_mode("never")  # Uncomment to disable approval requirement
        run = agents_client.runs.create(thread_id=thread.id, agent_id=agent.id, tool_resources=mcp_tool.resources)
        print(f"Created run, run ID: {run.id}")

        while run.status in ["queued", "in_progress", "requires_action"]:
            time.sleep(1)
            run = agents_client.runs.get(thread_id=thread.id, run_id=run.id)

            if run.status == "requires_action" and isinstance(run.required_action, SubmitToolApprovalAction):
                tool_calls = run.required_action.submit_tool_approval.tool_calls
                if not tool_calls:
                    print("No tool calls provided - cancelling run")
                    agents_client.runs.cancel(thread_id=thread.id, run_id=run.id)
                    break

                tool_approvals = []
                for tool_call in tool_calls:
                    if isinstance(tool_call, RequiredMcpToolCall):
                        try:
                            print(f"Approving tool call: {tool_call}")
                            tool_approvals.append(
                                ToolApproval(
                                    tool_call_id=tool_call.id,
                                    approve=True,
                                    headers=mcp_tool.headers,
                                )
                            )
                        except Exception as e:
                            print(f"Error approving tool_call {tool_call.id}: {e}")

                print(f"tool_approvals: {tool_approvals}")
                if tool_approvals:
                    agents_client.runs.submit_tool_outputs(
                        thread_id=thread.id, run_id=run.id, tool_approvals=tool_approvals
                    )

            print(f"Current run status: {run.status}")
            # [END handle_tool_approvals]

        print(f"Run finished with status: {run.status}")

        if run.status == "failed":
            # Check if you got "Rate limit is exceeded.", then you want to get more quota
            print(f"Run failed: {run.last_error}")

        # Display run steps and tool calls
        run_steps = agents_client.run_steps.list(thread_id=thread.id, run_id=run.id)

        # Loop through each step
        for step in run_steps:
            print(f"Step {step['id']} status: {step['status']}")

            # Check if there are tool calls in the step details
            step_details = step.get("step_details", {})
            tool_calls = step_details.get("tool_calls", [])

            if tool_calls:
                print("  MCP Tool calls:")
                for call in tool_calls:
                    print(f"    Tool Call ID: {call.get('id')}")
                    print(f"    Type: {call.get('type')}")

            print()  # add an extra newline between steps

        # Fetch and log all messages
        messages = agents_client.messages.list(thread_id=thread.id, order=ListSortOrder.ASCENDING)
        print("\nConversation:")
        print("-" * 50)
        for msg in messages:
            if msg.text_messages:
                last_text = msg.text_messages[-1]
                print(f"{msg.role.upper()}: {last_text.text.value}")
                print("-" * 50)

        # Example of dynamic tool management
        print(f"\nDemonstrating dynamic tool management:")
        print(f"Current allowed tools: {mcp_tool.allowed_tools}")

        # Remove a tool
        try:
            mcp_tool.disallow_tool(search_api_code)
            print(f"After removing {search_api_code}: {mcp_tool.allowed_tools}")
        except ValueError as e:
            print(f"Error removing tool: {e}")

        # Clean-up and delete the agent once the run is finished.
        # Controlled by DELETE_AGENT_ON_EXIT environment variable
        delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
        if delete_on_exit:
            agents_client.delete_agent(agent.id)
            print("Deleted agent")
        else:
            print(f"Agent {agent.id} preserved for examination in Azure AI Foundry")


if __name__ == '__main__':
    main()
