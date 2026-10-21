"""
Multimodal capabilities demo using Azure AI Foundry SDK.

This demo shows how to process images with vision capabilities
using Azure AI Agents.
"""

import asyncio
import os
import base64
from pathlib import Path
from dotenv import load_dotenv

from azure.identity.aio import DefaultAzureCredential
from azure.ai.projects.aio import AIProjectClient
from azure.ai.agents.aio import AgentsClient

# Load environment variables
load_dotenv()

PROJECT_ENDPOINT = os.getenv("PROJECT_ENDPOINT")
MODEL_DEPLOYMENT = os.getenv("MODEL_DEPLOYMENT")
DATA_PATH = os.getenv("DATA_PATH", "./data")


async def main():
    """Demo: Process image with vision capabilities."""
    
    print("\n" + "="*70)
    print("DEMO: Multimodal Processing (Azure AI Foundry SDK)")
    print("="*70)
    
    # Check for invoice image
    invoice_path = Path(DATA_PATH) / "invoice.jpg"
    if not invoice_path.exists():
        print(f"\nERROR: Invoice image not found at {invoice_path}")
        print("Please ensure invoice.jpg exists in the data folder.")
        return
    
    print(f"\nFound invoice image: {invoice_path}")
    
    async with DefaultAzureCredential() as credential:
        async with AIProjectClient(
            endpoint=PROJECT_ENDPOINT,
            credential=credential
        ) as project_client:
            
            async with AgentsClient(
                endpoint=PROJECT_ENDPOINT,
                credential=credential
            ) as agents_client:
                
                print("\nCreating vision-capable agent...")
                
                # Note: For vision capabilities, we need a vision-capable model
                # like gpt-4o or gpt-4-vision-preview
                agent = await agents_client.create_agent(
                    model=MODEL_DEPLOYMENT,  # Should be a vision-capable model
                    name="Vision Agent",
                    instructions=(
                        "You are a helpful assistant with vision capabilities. "
                        "Analyze images carefully and extract relevant information."
                    ),
                    description="Agent with multimodal vision capabilities"
                )
                
                print(f"Agent created: {agent.id}")
                
                # Create thread
                thread = await agents_client.create_thread()
                print(f"Thread created: {thread.id}")
                
                # Read and encode the image
                print("\nReading and encoding image...")
                with open(invoice_path, "rb") as img_file:
                    image_data = base64.b64encode(img_file.read()).decode('utf-8')
                
                # Create message with image
                print("Sending image to agent for analysis...")
                
                message = await agents_client.create_message(
                    thread_id=thread.id,
                    role="user",
                    content="Please analyze this invoice image and extract key information like invoice number, date, amounts, and items."
                )
                
                # Upload the image as a file for the agent to access
                # Note: Azure AI Agents supports file attachments
                print("Uploading image file...")
                
                file = await agents_client.upload_file(
                    file=open(invoice_path, "rb"),
                    purpose="assistants"
                )
                
                print(f"File uploaded: {file.id}")
                
                # Create a new message with file attachment
                message_with_image = await agents_client.create_message(
                    thread_id=thread.id,
                    role="user",
                    content="Analyze the attached invoice image.",
                    attachments=[{
                        "file_id": file.id,
                        "tools": [{"type": "code_interpreter"}]
                    }]
                )
                
                # Create and process run
                print("\nAgent analyzing image...")
                print("Agent: ", end="", flush=True)
                
                async with await agents_client.create_run_stream(
                    thread_id=thread.id,
                    assistant_id=agent.id
                ) as stream:
                    async for event in stream:
                        if event.event == "thread.message.delta":
                            if hasattr(event.data, 'delta') and hasattr(event.data.delta, 'content'):
                                for content in event.data.delta.content:
                                    if hasattr(content, 'text') and hasattr(content.text, 'value'):
                                        print(content.text.value, end="", flush=True)
                
                print("\n")
                
                # Clean up
                print("\nCleaning up...")
                await agents_client.delete_file(file.id)
                await agents_client.delete_agent(agent.id)
                print("Resources cleaned up.")
                
                print("\n" + "="*70)
                print("DEMO COMPLETE")
                print("="*70)
                print("The agent processed the image using vision capabilities")
                print("In a full application, you could extract structured data")
                print("="*70 + "\n")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nSee you again soon.")
    except Exception as e:
        print(f"\nError: {e}")
