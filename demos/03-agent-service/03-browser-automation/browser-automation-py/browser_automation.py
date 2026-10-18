import os
import json
from dotenv import load_dotenv
from azure.identity import DefaultAzureCredential
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import (
    PromptAgentDefinition,
    BrowserAutomationTool,
    BrowserAutomationToolParameters,
    BrowserAutomationToolConnectionParameters,
)

load_dotenv(override=True)

INSTRUCTIONS = (
    "You automate browser tasks with the Browser Automation tool. "
    "Create one browser session, reuse it for every step, and close it when the task is done. "
    "Base every answer on what the browser shows, never on prior knowledge."
)

TASKS = [
    "Create a browser session and give me the live view link. Do not browse anywhere yet.",
    "Open https://example.com in that session. Report the page title and the description paragraph.",
    "Follow the link in the description paragraph and report the heading of the page it leads to. Then close the browser session.",
]

project = AIProjectClient(endpoint=os.environ["PROJECT_ENDPOINT"], credential=DefaultAzureCredential())
openai = project.get_openai_client()

tool = BrowserAutomationTool(
    browser_automation=BrowserAutomationToolParameters(
        connection=BrowserAutomationToolConnectionParameters(
            project_connection_id=os.environ["BROWSER_CONNECTION_ID"]
        )
    )
)

agent = project.agents.create_version(
    agent_name="m3-browser-agent",
    definition=PromptAgentDefinition(
        model=os.environ["MODEL_DEPLOYMENT_NAME"],
        instructions=INSTRUCTIONS,
        tools=[tool],
    ),
)
print(f"Created {agent.name}:{agent.version}")

conversation = openai.conversations.create()

for number, task in enumerate(TASKS, start=1):
    print(f"== Task {number}: {task}")
    stream = openai.responses.create(
        stream=True,
        conversation=conversation.id,
        input=task,
        extra_body={"agent_reference": {"name": agent.name, "version": agent.version, "type": "agent_reference"}},
    )
    for event in stream:
        if event.type == "response.output_item.done" and event.item.type == "remote_function_call":
            arguments = json.loads(getattr(event.item, "arguments", None) or "{}")
            arguments.pop("browserSessionId", None)
            print(f"browser action: {event.item.name} {arguments}")
        elif event.type == "response.output_text.done":
            print("agent: live view URL received (kept private)" if "live.playwright.microsoft.com" in event.text else f"agent: {event.text}")

openai.conversations.delete(conversation.id)
project.agents.delete_version(agent_name=agent.name, agent_version=agent.version)
print(f"Deleted {agent.name}:{agent.version}")
