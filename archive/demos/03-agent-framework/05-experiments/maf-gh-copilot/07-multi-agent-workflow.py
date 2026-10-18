"""
Multi-agent workflow with GitHub Copilot example.

This demo shows how to combine GitHub Copilot with other agents (like Azure OpenAI)
in a multi-agent workflow. An Azure OpenAI agent drafts a marketing tagline
and a GitHub Copilot agent reviews it.
"""

import asyncio

from azure.identity import AzureCliCredential

from agent_framework import Message, Role
from agent_framework.orchestrations import SequentialBuilder
from agent_framework.azure import AzureOpenAIChatClient
from agent_framework.github import GitHubCopilotAgent


async def main():
    # Create an Azure OpenAI agent as a copywriter
    chat_client = AzureOpenAIChatClient(credential=AzureCliCredential())

    writer = chat_client.as_agent(
        instructions="You are a concise copywriter. Provide a single, punchy marketing sentence based on the prompt.",
        name="writer",
    )

    # Create a GitHub Copilot agent as a reviewer
    reviewer = GitHubCopilotAgent(
        default_options={"instructions": "You are a thoughtful reviewer. Give brief feedback on the previous assistant message."},
        name="reviewer",
    )

    # Build a sequential workflow: writer -> reviewer
    workflow = SequentialBuilder().participants([writer, reviewer]).build()

    # Run the workflow
    output_evt = None
    async for event in workflow.run_stream("Write a tagline for a budget-friendly electric bike."):
        if event.type == "output":
            output_evt = event

    # Display final conversation
    if output_evt:
        print("===== Final Conversation =====")
        messages: list[Message] = output_evt.data
        for i, msg in enumerate(messages, start=1):
            name = msg.author_name or ("assistant" if msg.role == Role.ASSISTANT else "user")
            print(f"{'-' * 60}\n{i:02d} [{name}]\n{msg.text}\n")


if __name__ == "__main__":
    asyncio.run(main())
