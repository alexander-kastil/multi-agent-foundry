"""
NEW 09: Multimodal Content - Image (JPEG) Invoice Extraction (Demo)

This demo shows how to process a JPEG invoice image and extract structured
data using Azure OpenAI's vision capabilities with the Agent Framework.
The agent loads the invoice image directly and extracts invoice data as JSON.
PDF conversion logic removed; relies solely on `invoice.jpg`.
"""

import asyncio
import os
import json
import base64
from pathlib import Path
from pydantic import BaseModel, Field
from dotenv import load_dotenv

from agent_framework.azure import AzureOpenAIChatClient
from agent_framework._types import ChatMessage, TextContent, UriContent

# Load environment variables
load_dotenv()

ENDPOINT = os.getenv("AZURE_OPENAI_ENDPOINT")
DEPLOYMENT = os.getenv("AZURE_OPENAI_CHAT_DEPLOYMENT_NAME")
API_KEY = os.getenv("AZURE_OPENAI_API_KEY")
API_VERSION = os.getenv("AZURE_OPENAI_API_VERSION", "2024-07-01-preview")
DATA_PATH = os.getenv("DATA_PATH", "./data")
OUTPUT_PATH = os.getenv("OUTPUT_PATH", "./output")


# Define structured output model for invoice data
class InvoiceItem(BaseModel):
    """Individual line item on invoice."""
    item: str
    description: str | None = None
    quantity: float | None = None
    unit_price: float | None = None
    total: float | None = None


class InvoiceData(BaseModel):
    """Extract complete invoice information from document."""
    invoice_number: str | None = Field(None, description="Invoice number")
    date: str | None = Field(None, description="Invoice date")
    due_date: str | None = Field(None, description="Payment due date")
    vendor_name: str | None = Field(None, description="Vendor/company name")
    customer_name: str | None = Field(None, description="Customer name")
    items: list[InvoiceItem] = Field(default_factory=list, description="Line items")
    subtotal: float | None = Field(None, description="Subtotal amount")
    tax: float | None = Field(None, description="Tax amount")
    total: float | None = Field(None, description="Total amount")
    payment_terms: str | None = Field(None, description="Payment terms")





async def main():
    """Demo: Extract structured invoice data from PDF."""
    
    print("\n" + "="*70)
    print("📄 DEMO: Multimodal JPEG Invoice Extraction")
    print("="*70)
    
    # Ensure output directory exists
    Path(OUTPUT_PATH).mkdir(parents=True, exist_ok=True)
    
    # Check for invoice JPEG
    invoice_image_path = os.path.join(DATA_PATH, "invoice.jpg")
    if not os.path.exists(invoice_image_path):
        print(f"\n❌ Error: Invoice image not found at {invoice_image_path}")
        print("Please ensure invoice.jpg exists in the data directory.")
        return

    print(f"\n📂 Loading invoice image: {invoice_image_path}")

    # Load JPEG as base64 data URI (no PDF conversion)
    print("🔄 Preparing JPEG data URI...")
    with open(invoice_image_path, "rb") as f:
        b64_image = base64.b64encode(f.read()).decode("utf-8")
    image_data_url = f"data:image/jpeg;base64,{b64_image}"
    print("✅ JPEG loaded and encoded to data URI")
    
    # =================================================================
    # OPTION 1: WITH STRUCTURED OUTPUT (Using Pydantic Model)
    # =================================================================
    print("\n" + "="*70)
    print("📋 OPTION 1: With Structured Output (Pydantic Model)")
    print("="*70)
    
    agent_structured = AzureOpenAIChatClient(
        endpoint=ENDPOINT,
        deployment_name=DEPLOYMENT,
        api_key=API_KEY,
        api_version=API_VERSION
    ).create_agent(
        instructions=(
            "You are an expert invoice processing assistant. "
            "Extract all invoice information accurately from the provided image. "
            "Pay attention to all fields including items, prices, and totals."
        ),
        name="InvoiceExtractor_Structured"
    )
    
    user_message_structured = ChatMessage(
        role="user",
        contents=[
            TextContent(text="Please extract all invoice information from this document and return it as structured data."),
            UriContent(uri=image_data_url, media_type="image/png")
        ]
    )
    
    print("\n🔄 Processing JPEG with structured output (response_format=InvoiceData)...")
    response_structured = await agent_structured.run(user_message_structured, response_format=InvoiceData)
    
    if response_structured.value:
        invoice = response_structured.value
        
        print("\n" + "="*70)
        print("📊 Extracted Invoice Information:")
        print("="*70)
        
        print(f"\n📋 Invoice Details:")
        print(f"   Invoice Number: {invoice.invoice_number}")
        print(f"   Date: {invoice.date}")
        print(f"   Due Date: {invoice.due_date}")
        print(f"   Vendor: {invoice.vendor_name}")
        print(f"   Customer: {invoice.customer_name}")
        
        if invoice.items:
            print(f"\n📦 Items ({len(invoice.items)}):")
            for i, item in enumerate(invoice.items, 1):
                print(f"   {i}. {item.item}")
                if item.description:
                    print(f"      Description: {item.description}")
                if item.quantity is not None and item.unit_price is not None:
                    print(f"      Qty: {item.quantity} × ${item.unit_price:.2f} = ${item.total:.2f}")
        
        print(f"\n💰 Totals:")
        if invoice.subtotal is not None:
            print(f"   Subtotal: ${invoice.subtotal:.2f}")
        if invoice.tax is not None:
            print(f"   Tax: ${invoice.tax:.2f}")
        if invoice.total is not None:
            print(f"   Total: ${invoice.total:.2f}")
        
        if invoice.payment_terms:
            print(f"\n📝 Payment Terms: {invoice.payment_terms}")
        
        # Save to JSON file
        output_file_structured = os.path.join(OUTPUT_PATH, "invoice_structured.json")
        with open(output_file_structured, 'w') as f:
            json.dump(invoice.model_dump(), f, indent=2)
        
        print(f"\n✅ Structured data saved to: {output_file_structured}")
        
    else:
        print("\n❌ Could not extract invoice information (structured)")
    
    # =================================================================
    # OPTION 2: WITHOUT STRUCTURED OUTPUT (Schemaless - Just Prompting)
    # =================================================================
    print("\n" + "="*70)
    print("📋 OPTION 2: Without Structured Output (Schemaless - Prompt Only)")
    print("="*70)
    
    agent_unstructured = AzureOpenAIChatClient(
        endpoint=ENDPOINT,
        deployment_name=DEPLOYMENT,
        api_key=API_KEY,
        api_version=API_VERSION
    ).create_agent(
        instructions=(
            "You are an expert invoice processing assistant. "
            "Extract all invoice information accurately from the provided image. "
            "ALWAYS return your response as valid JSON with this structure:\n"
            "{\n"
            '  "invoice_number": "string",\n'
            '  "date": "string",\n'
            '  "due_date": "string",\n'
            '  "vendor_name": "string",\n'
            '  "customer_name": "string",\n'
            '  "items": [{"item": "string", "description": "string", "quantity": number, "unit_price": number, "total": number}],\n'
            '  "subtotal": number,\n'
            '  "tax": number,\n'
            '  "total": number,\n'
            '  "payment_terms": "string"\n'
            "}\n"
            "Return ONLY the JSON, no markdown code blocks or extra text."
        ),
        name="InvoiceExtractor_Unstructured"
    )
    
    user_message_unstructured = ChatMessage(
        role="user",
        contents=[
            TextContent(text="Extract all invoice information from this document and return as JSON."),
            UriContent(uri=image_data_url, media_type="image/png")
        ]
    )
    
    print("\n🔄 Processing JPEG without structured output (no response_format)...")
    response_unstructured = await agent_unstructured.run(user_message_unstructured)
    
    if response_unstructured.text:
        print("\n📄 Raw Response (first 500 chars):")
        print(response_unstructured.text[:500] + "..." if len(response_unstructured.text) > 500 else response_unstructured.text)
        
        # Try to parse the JSON from the response
        try:
            # Clean up potential markdown code blocks
            json_text = response_unstructured.text.strip()
            if json_text.startswith("```"):
                # Remove markdown code blocks
                json_text = json_text.split("```")[1]
                if json_text.startswith("json"):
                    json_text = json_text[4:]
                json_text = json_text.strip()
            
            invoice_data = json.loads(json_text)
            
            print("\n✅ Successfully parsed JSON!")
            print("\n📊 Extracted Invoice Information:")
            print(f"   Invoice Number: {invoice_data.get('invoice_number')}")
            print(f"   Date: {invoice_data.get('date')}")
            print(f"   Vendor: {invoice_data.get('vendor_name')}")
            print(f"   Customer: {invoice_data.get('customer_name')}")
            print(f"   Total: ${invoice_data.get('total')}")
            
            # Save to JSON file
            output_file_unstructured = os.path.join(OUTPUT_PATH, "invoice_unstructured.json")
            with open(output_file_unstructured, 'w') as f:
                json.dump(invoice_data, f, indent=2)
            
            print(f"\n✅ Unstructured data saved to: {output_file_unstructured}")
            
        except json.JSONDecodeError as e:
            print(f"\n❌ Failed to parse JSON: {e}")
            print("⚠️  The LLM didn't return valid JSON - this is the risk of schemaless!")
        
    else:
        print("\n❌ No response text received")
    
    # =================================================================
    # COMPARISON SUMMARY
    # =================================================================
    print("\n" + "="*70)
    print("📊 COMPARISON: Schema (Model) vs Schemaless (No Model)")
    print("="*70)
    print("\n✅ WITH Pydantic Model (response_format=InvoiceData):")
    print("   • LLM is FORCED to return data matching your exact schema")
    print("   • Guaranteed JSON structure - no parsing errors")
    print("   • Type validation and conversion (strings, floats, etc.)")
    print("   • Direct access to typed fields: invoice.invoice_number")
    print("   • Handles optional fields with None values")
    print("   • Production-ready and reliable")
    print("\n⚠️  WITHOUT Pydantic Model (schemaless - just prompting):")
    print("   • LLM MIGHT wrap JSON in markdown code blocks")
    print("   • No guaranteed structure - fields might be missing")
    print("   • Need manual JSON parsing with try/except error handling")
    print("   • LLM might ignore your format request")
    print("   • More fragile and error-prone")
    print("   • Risk of inconsistent responses")
    print("\n🎯 RECOMMENDATION:")
    print("   Always use Pydantic models (schema) for structured data extraction!")
    print("   The model IS the schema - it's not optional for production use.")
    
    print("\n" + "="*70)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\n👋 Process interrupted.")
    except Exception as e:
        print(f"\n❌ Error: {e}")