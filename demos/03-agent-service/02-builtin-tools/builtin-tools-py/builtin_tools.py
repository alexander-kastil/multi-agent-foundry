import os
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import (
    PromptAgentDefinition,
    CodeInterpreterTool,
    AutoCodeInterpreterToolParam,
    FileSearchTool,
    WebSearchTool,
)

load_dotenv(override=True)

MODEL = os.environ["MODEL_DEPLOYMENT_NAME"]
project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai = project.get_openai_client()
here = os.path.dirname(os.path.abspath(__file__))


def ask(agent, question):
    return openai.responses.create(
        input=question,
        extra_body={"agent_reference": {"name": agent.name, "version": agent.version, "type": "agent_reference"}},
    )


print("== Step 1: Code Interpreter")
with open(os.path.join(here, "assets", "quarterly_results.csv"), "rb") as f:
    csv_file = openai.files.create(purpose="assistants", file=f)
analyst = project.agents.create_version(
    agent_name="m3-analyst",
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="You are a data analyst. Use Python to analyze the uploaded file and save charts as PNG files.",
        tools=[CodeInterpreterTool(container=AutoCodeInterpreterToolParam(file_ids=[csv_file.id]))],
    ),
)
response = ask(analyst, "Create a bar chart of operating_profit for the TRANSPORTATION sector companies and give me the file.")
for item in response.output:
    print(f"output item: {item.type}")
message = response.output[-1]
citation = [a for a in message.content[-1].annotations if a.type == "container_file_citation"][-1]
content = openai.containers.files.content.retrieve(file_id=citation.file_id, container_id=citation.container_id)
chart_path = os.path.join(here, citation.filename)
with open(chart_path, "wb") as f:
    f.write(content.read())
print(f"Downloaded {citation.filename} ({os.path.getsize(chart_path)} bytes)")

print("== Step 2: File Search")
store = openai.vector_stores.create(name="m3-product-docs")
with open(os.path.join(here, "assets", "product-info.md"), "rb") as f:
    doc = openai.vector_stores.files.upload_and_poll(vector_store_id=store.id, file=f)
print(f"Vector store: {store.id}")
support = project.agents.create_version(
    agent_name="m3-product-agent",
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="Answer only from the product documents. Cite the file you used.",
        tools=[FileSearchTool(vector_store_ids=[store.id])],
    ),
)
response = ask(support, "How long is the warranty and what is the return policy?")
print(response.output_text)
for item in response.output:
    print(f"output item: {item.type}")
for annotation in response.output[-1].content[-1].annotations:
    print(f"citation: {annotation.type} {getattr(annotation, 'filename', '')}")

print("== Step 3: Web Search")
scout = project.agents.create_version(
    agent_name="m3-web-agent",
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="Use web search for current facts and cite your sources.",
        tools=[WebSearchTool()],
    ),
)
response = ask(scout, "What is the latest stable version of the azure-ai-projects Python package on PyPI?")
print(response.output_text)
for item in response.output:
    print(f"output item: {item.type}")
for annotation in response.output[-1].content[-1].annotations:
    if annotation.type == "url_citation":
        print(f"citation: {annotation.url}")

print("== Step 4: Combine tools in one agent")
combined = project.agents.create_version(
    agent_name="m3-combined-agent",
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="Use file search for product facts and Python for any calculation.",
        tools=[
            FileSearchTool(vector_store_ids=[store.id]),
            CodeInterpreterTool(container=AutoCodeInterpreterToolParam()),
        ],
    ),
)
response = ask(combined, "The glasses warranty is in the docs. How many days does the warranty last, assuming 365 days per year? Compute it with Python.")
print(response.output_text)
print("tools used: " + ", ".join(i.type for i in response.output))

print("== Cleanup")
for agent in (analyst, support, scout, combined):
    project.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
    print(f"Deleted {agent.name}:{agent.version}")
openai.vector_stores.delete(store.id)
openai.files.delete(csv_file.id)
openai.files.delete(doc.id)
os.remove(chart_path)
