import os
import io
import sys
from dotenv import load_dotenv
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition, MCPTool
from azure.identity import DefaultAzureCredential, get_bearer_token_provider

# Configure UTF-8 encoding for Windows console (fixes emoji display issues)
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')


def log(message: str) -> None:
    """Print log message if detailed logging is enabled."""
    detailed_logging = os.getenv("DETAILED_LOGGING", "false").lower() == "true"
    if detailed_logging:
        print(f"[LOG] {message}")


def main():
    # Clear the console to keep the output focused on the agent interaction
    os.system('cls' if os.name == 'nt' else 'clear')

    # Load environment variables from .env file
    load_dotenv()
    endpoint = os.getenv("PROJECT_ENDPOINT")
    model = os.getenv("MODEL_DEPLOYMENT")
    search_connection = os.getenv("AZURE_AI_SEARCH_CONNECTION")
    knowledge_base_name = os.getenv("KNOWLEDGE_BASE_NAME", "my-knowledge-base")
    project_connection_name = os.getenv("FOUNDRY_IQ_CONNECTION", "foundry-iq-connection")

    print(f"Using endpoint: {endpoint}")
    print(f"Using model: {model}")
    print(f"Knowledge base: {knowledge_base_name}")
    log(f"Detailed logging enabled")
    log(f"Environment loaded: endpoint={endpoint}, model={model}, kb={knowledge_base_name}")

    credential = DefaultAzureCredential()

    # Create project client
    project_client = AIProjectClient(
        endpoint=endpoint,
        credential=credential,
    )
    log("Created AIProjectClient")

    # Get the search service endpoint from the connection
    search_conn = project_client.connections.get(search_connection)
    search_endpoint = search_conn.properties.get("target")
    print(f"Search endpoint: {search_endpoint}")
    log(f"Retrieved search endpoint: {search_endpoint}")

    # Construct MCP endpoint for knowledge base
    mcp_endpoint = f"{search_endpoint}/knowledgebases/{knowledge_base_name}/mcp?api-version=2025-11-01-preview"
    log(f"MCP endpoint: {mcp_endpoint}")

    # Define optimized agent instructions for knowledge base retrieval
    # These instructions maximize MCP tool invocation and ensure proper citation formatting
    instructions = """
You are a helpful assistant that must use the knowledge base to answer all the questions from user. You must never answer from your own knowledge under any circumstances.
Every answer must always provide annotations for using the MCP knowledge base tool and render them as: `【message_idx:search_idx†source_name】`
If you cannot find the answer in the provided knowledge base you must respond with "I don't know".
"""
    log("Agent instructions configured for optimal knowledge base retrieval")

    # Create MCP tool with knowledge base connection
    # The knowledge base orchestrates query planning, decomposition, and retrieval
    mcp_kb_tool = MCPTool(
        server_label="knowledge-base",
        server_url=mcp_endpoint,
        require_approval="never",
        allowed_tools=["knowledge_base_retrieve"],
        project_connection_id=project_connection_name
    )
    log(f"Created MCPTool with server_label='knowledge-base', allowed_tools=['knowledge_base_retrieve']")

    # Create agent with MCP knowledge base tool
    with project_client:
        agent = project_client.agents.create_version(
            agent_name="foundry-iq-agent",
            definition=PromptAgentDefinition(
                model=model,
                instructions=instructions,
                tools=[mcp_kb_tool]
            )
        )
        print(f"Created agent: {agent.name}, version: {agent.version}")
        log(f"Agent created with MCP knowledge base tool")

        # Get OpenAI client for conversations
        openai_client = project_client.get_openai_client()
        log("Created OpenAI client")

        # Create conversation
        conversation = openai_client.conversations.create()
        print(f"Created conversation: {conversation.id}")
        log(f"Conversation created: {conversation.id}")

        # Send query to agent
        # The agent will invoke the knowledge base via MCP to retrieve relevant content
        user_query = "What are the key benefits of using Azure AI Search for knowledge retrieval?"
        print(f"\nUser query: {user_query}")
        log(f"Sending query: {user_query}")

        response = openai_client.responses.create(
            conversation=conversation.id,
            input=user_query,
            extra_body={"agent": {"name": agent.name, "type": "agent_reference"}},
        )

        print(f"\nAgent response:")
        print("-" * 50)
        print(response.output_text)
        print("-" * 50)
        log(f"Response received, length: {len(response.output_text)}")

        # Delete the agent when done (if configured)
        delete_on_exit = os.getenv("DELETE_AGENT_ON_EXIT", "true").lower() == "true"
        if delete_on_exit:
            project_client.agents.delete_version(agent.name, agent.version)
            print(f"\nDeleted agent: {agent.name} version {agent.version}")
        else:
            print(f"\nAgent {agent.name} version {agent.version} preserved for examination in Azure AI Foundry")

        print(f"\nYou can examine the conversation using this id: {conversation.id}")


if __name__ == '__main__':
    main()
