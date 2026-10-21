"""
Delete all agents in the Azure AI Foundry project.

This utility script deletes all agents to clean up resources.
"""

import asyncio
from azure.identity.aio import DefaultAzureCredential
from azure.ai.agents.aio import AgentsClient
from dotenv import load_dotenv
import os

load_dotenv()

PROJECT_ENDPOINT = os.getenv("PROJECT_ENDPOINT")


async def main():
    print("\n" + "="*70)
    print("DELETE ALL AGENTS")
    print("="*70)
    
    async with DefaultAzureCredential() as credential:
        async with AgentsClient(
            endpoint=PROJECT_ENDPOINT,
            credential=credential
        ) as agents_client:
            
            print("\nListing all agents...")
            agents = await agents_client.list_agents()
            
            if not agents.data:
                print("No agents found.")
                return
            
            print(f"\nFound {len(agents.data)} agent(s):")
            for agent in agents.data:
                print(f"  - {agent.name} (ID: {agent.id})")
            
            confirm = input("\nDelete all agents? (yes/no): ").strip().lower()
            
            if confirm == 'yes':
                print("\nDeleting agents...")
                for agent in agents.data:
                    await agents_client.delete_agent(agent.id)
                    print(f"  ✓ Deleted: {agent.name}")
                
                print("\nAll agents deleted successfully!")
            else:
                print("\nCancelled. No agents were deleted.")


if __name__ == "__main__":
    asyncio.run(main())
