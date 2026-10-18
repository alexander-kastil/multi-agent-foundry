# Migration to Microsoft Foundry Agents API - Final Outcome

## ✅ Migration Successful

The Python agent has been successfully migrated from the old Azure AI Agents API to the new Microsoft Foundry Agents API. The agent now uses:

- **Agent Creation**: `create_version()` with `PromptAgentDefinition` instead of `create_agent()`
- **Responses API**: Direct responses with `openai_client.responses.create()` instead of threads and runs
- **Image Input**: Base64 encoded images via `input_image` with `image_url` parameter

### Key Changes Made

1. **Agent Creation**

   - **Before**: `agents_client.create_agent(model, name, instructions)`
   - **After**: `project_client.agents.create_version(agent_name, definition=PromptAgentDefinition(model, instructions))`

2. **Image Handling**

   - **Before**: Upload file, create thread, add message with image blocks, create run
   - **After**: Base64 encode image, create response with input containing image_url

3. **Response Processing**

   - **Before**: Create run, poll for completion, list messages
   - **After**: Create response directly, check status, access output items

4. **Cleanup**
   - **Before**: `delete_agent(agent.id)`
   - **After**: `delete_version(agent_name, agent_version)`

---

## Migration Process Documentation

### Overview

This document details the migration of `agent-input-file.py` from the legacy Azure AI Agents API to the new Microsoft Foundry Agents API (Responses API).

### Migration Steps

#### Step 1: Updated Imports

**Before:**

```python
from azure.ai.agents import AgentsClient
from azure.ai.agents.models import (
    ListSortOrder,
    MessageTextContent,
    MessageInputContentBlock,
    MessageImageFileParam,
    MessageInputTextBlock,
    MessageInputImageFileBlock,
    FilePurpose,
    RunStatus,
)
```

**After:**

```python
from azure.ai.projects import AIProjectClient
from azure.ai.projects.models import PromptAgentDefinition
import base64
```

**Rationale:** The new API uses `AIProjectClient` from `azure-ai-projects` package which provides access to both the agents operations and the OpenAI client for responses.

---

#### Step 2: Client Initialization

**Before:**

```python
agents_client = AgentsClient(
    endpoint=endpoint,
    credential=DefaultAzureCredential()
)
```

**After:**

```python
project_client = AIProjectClient(
    endpoint=endpoint,
    credential=DefaultAzureCredential()
)
openai_client = project_client.get_openai_client()
```

**Rationale:** The new API separates agent management (via `project_client.agents`) from conversation/response operations (via `openai_client`).

---

#### Step 3: Agent Creation

**Before:**

```python
agent = agents_client.create_agent(
    model=model,
    name="file-search-agent-mig",
    instructions="You are helpful agent",
)
```

**After:**

```python
agent = project_client.agents.create_version(
    agent_name="file-search-agent-mig",
    definition=PromptAgentDefinition(
        model=model,
        instructions="You are helpful agent",
    )
)
```

**Rationale:** The new API uses versioned agents with `create_version()` and requires a definition object that specifies the agent kind (prompt, workflow, or hosted).

---

#### Step 4: Image Handling

**Before:**

```python
image_file = agents_client.files.upload_and_poll(
    file_path=asset_file_path,
    purpose=FilePurpose.AGENTS
)

file_param = MessageImageFileParam(file_id=image_file.id, detail="high")
content_blocks = [
    MessageInputTextBlock(text=input_message),
    MessageInputImageFileBlock(image_file=file_param),
]
```

**After:**

```python
# Encode image as base64
with open(asset_file_path, "rb") as image_file:
    base64_image = base64.b64encode(image_file.read()).decode('utf-8')
```

**Rationale:** The Responses API with agents supports base64 encoded images directly in the input. File uploads with `input_file` type only support PDF files, not images.

---

#### Step 5: Thread and Message Creation → Response Creation

**Before:**

```python
thread = agents_client.threads.create()
message = agents_client.messages.create(
    thread_id=thread.id,
    role="user",
    content=content_blocks
)
```

**After:**

```python
# No separate thread/message creation needed
# Messages are part of the response input
```

**Rationale:** The new Responses API doesn't require explicit thread and message creation. Input messages are provided directly in the response creation call.

---

#### Step 6: Run Processing → Response Creation

**Before:**

```python
run = agents_client.runs.create_and_process(
    thread_id=thread.id,
    agent_id=agent.id
)

if run.status != RunStatus.COMPLETED:
    print(f"The run did not succeed: {run.status=}.")
```

**After:**

```python
response = openai_client.responses.create(
    input=[
        {
            "role": "user",
            "content": [
                {"type": "input_text", "text": input_message},
                {
                    "type": "input_image",
                    "image_url": f"data:image/jpeg;base64,{base64_image}"
                }
            ]
        }
    ],
    extra_body={
        "agent": {
            "type": "agent_reference",
            "name": agent.name,
            "version": agent.version
        }
    }
)

if response.status != "completed":
    print(f"The response did not succeed: {response.status}")
```

**Rationale:** The Responses API is synchronous by default and returns immediately. The agent is referenced via `extra_body` parameter with agent name and version.

---

#### Step 7: Output Retrieval

**Before:**

```python
messages = agents_client.messages.list(
    thread_id=thread.id,
    order=ListSortOrder.ASCENDING
)
for msg in messages:
    last_part = msg.content[-1]
    if isinstance(last_part, MessageTextContent):
        print(f"{msg.role}: {last_part.text.value}")
```

**After:**

```python
for output_item in response.output:
    if output_item.type == "message":
        print(f"{output_item.role}: {output_item.content[0].text}")
```

**Rationale:** Response output is directly accessible via the `output` property, which contains message items with their content.

---

#### Step 8: Cleanup

**Before:**

```python
agents_client.delete_agent(agent.id)
```

**After:**

```python
project_client.agents.delete_version(
    agent_name=agent.name,
    agent_version=agent.version
)
```

**Rationale:** With versioned agents, deletion requires both agent name and version number.

---

### Key API Differences

| Aspect                 | Old API (Assistants)     | New API (Responses with Agents)          |
| ---------------------- | ------------------------ | ---------------------------------------- |
| **Agent Creation**     | `create_agent()`         | `create_version()` with definition       |
| **Conversation Model** | Threads + Messages       | Direct input in responses                |
| **Execution Model**    | Asynchronous Runs        | Synchronous Responses                    |
| **Context Management** | Thread-based             | Conversation-based (stateful by default) |
| **Agent Reference**    | By ID                    | By name + version                        |
| **Image Support**      | File upload with file_id | Base64 encoding or URL                   |

---

### Issues Encountered and Solutions

#### Issue 1: File Upload API

**Problem:** Initially tried to use `project_client.agents.files.upload_and_poll()` but `AgentsOperations` doesn't have a `files` attribute.

**Solution:** Switched to base64 encoding for image input, which is directly supported in the Responses API.

#### Issue 2: Image Input Format

**Problem:** Tried using `input_file` type with uploaded file, but it only supports PDF files.

**Solution:** Used `input_image` type with base64-encoded data URL: `data:image/jpeg;base64,{base64_image}`.

#### Issue 3: Content Structure

**Problem:** Initial attempt used `image_file` parameter structure from old API.

**Solution:** Updated to use `image_url` parameter with base64 data URL for the Responses API.

---

### Package Dependencies

No changes required to `requirements.txt`. The existing packages are correct:

- `azure-ai-projects==2.0.0b2` - Provides `AIProjectClient` and access to agents
- `azure-identity==1.25.1` - For authentication
- Existing `openai==2.8.1` - Used by the OpenAI client returned from `get_openai_client()`

---

### Testing Results

✅ **Agent Creation**: Successfully created versioned agent
✅ **Image Processing**: Successfully encoded and sent image
✅ **Response Generation**: Successfully received response from agent
✅ **Output Parsing**: Successfully extracted message content
✅ **Cleanup**: Successfully deleted agent version

---

### Benefits of New API

1. **Simplified Flow**: No need to manage threads, messages, and runs separately
2. **Versioning**: Better agent lifecycle management with versions
3. **Stateful by Default**: Automatic context retention across calls
4. **Better Performance**: Improved caching reduces costs
5. **Modern Architecture**: Built on Responses API instead of legacy Assistants API
6. **Future-Proof**: New features only added to this API

---

### Migration Checklist

- [x] Update client initialization
- [x] Migrate agent creation to `create_version()`
- [x] Replace thread/message/run flow with responses
- [x] Update image handling to base64 encoding
- [x] Update output processing
- [x] Update cleanup to use version deletion
- [x] Test end-to-end flow
- [x] Verify error handling

---

### References

- [Migration Guide](https://learn.microsoft.com/en-gb/azure/ai-foundry/agents/how-to/migrate?view=foundry)
- [Responses API Documentation](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/how-to/responses?view=foundry-classic)
- [Azure AI Projects SDK](https://learn.microsoft.com/en-us/python/api/overview/azure/ai-projects-readme?view=azure-python)

---

## Streaming Response Migration

This section documents the migration of Azure AI agent demo scripts from synchronous responses to streaming responses using the Responses API. The migration enables real-time output display and improved user experience.

### Overview

All demo scripts were updated to use `stream=True` in the `responses.create()` call, enabling event-driven output processing instead of waiting for complete responses.

### Common Changes Across All Scripts

#### 1. Enable Streaming

**Before:**

```python
response = client.responses.create(
    model=deployment_name,
    input=input_message,
    extra_body={
        "agent": {
            "type": "agent_reference",
            "name": agent.name,
            "version": agent.version
        }
    }
)
```

**After:**

```python
response = client.responses.create(
    model=deployment_name,
    input=input_message,
    stream=True,  # Enable streaming
    extra_body={
        "agent": {
            "type": "agent_reference",
            "name": agent.name,
            "version": agent.version
        }
    }
)
```

#### 2. Event-Based Output Processing

**Before:**

```python
if response.status == "completed":
    for output_item in response.output:
        if output_item.type == "message":
            print(f"{output_item.role}: {output_item.content[0].text}")
```

**After:**

```python
for event in response:
    if event.type == "output.delta":
        if event.delta.type == "message":
            if event.delta.content:
                print(event.delta.content[0].text, end="", flush=True)
    elif event.type == "output.completed":
        print()  # New line after completion
```

#### 3. Code Cleanup

Removed unnecessary imports that were no longer needed:

- `import io`
- `import sys`

Added explanatory comments for better code readability.

### Script-Specific Migrations

#### agent-basics.py

**Changes:**

- Added `stream=True` parameter
- Implemented event loop for real-time output
- Added comments explaining streaming flow
- Removed unused imports

**Key Fix:** Initial streaming implementation revealed that response attributes like `status` are not accessible on streaming responses.

#### agent-input-base64.py

**Changes:**

- Migrated to streaming responses
- Maintained base64 image encoding for input
- Updated output processing to event-based

**Pattern:** Base64 encoding remains the same, only output handling changed.

#### agent-input-file.py

**Changes:**

- Migrated to streaming
- Fixed AttributeError: `'ResponseStream' object has no attribute 'id'`
- Moved response attribute access outside the streaming loop

**Bug Fix:** Attempting to access `response.id` during iteration caused errors. Solution: Access attributes after streaming completes.

#### agent-input-url.py

**Changes:**

- Added streaming support
- Maintained URL-based image input
- Updated to event-driven output

**Pattern:** URL inputs work seamlessly with streaming responses.

#### agent-output.py

**Changes:**

- Implemented streaming for QR code generation
- Added real-time output display
- Maintained Azure Blob Storage integration

**Integration:** Streaming works with external service calls (QR code API).

#### agent-response-format.py

**Changes:**

- Migrated JSON response handling to streaming
- Updated parsing logic for streaming events
- Maintained structured output format

**Pattern:** JSON parsing adapted to accumulate content from streaming deltas.

#### agent-tracing.py

**Changes:**

- Added streaming support with OpenTelemetry tracing
- Fixed tracing attribute access on streaming responses
- Updated span creation and management

**Bug Fix:** Tracing attributes like `response.id` not available during streaming. Solution: Use alternative identifiers or defer tracing until completion.

### Code Patterns for Reuse

#### Basic Streaming Loop

```python
response = client.responses.create(
    model=deployment_name,
    input=input_message,
    stream=True,
    extra_body={"agent": {"type": "agent_reference", "name": agent.name, "version": agent.version}}
)

for event in response:
    if event.type == "output.delta":
        if event.delta.type == "message":
            if event.delta.content:
                print(event.delta.content[0].text, end="", flush=True)
    elif event.type == "output.completed":
        print()  # Final newline
```

#### Agent Creation with Versioning

```python
agent = project_client.agents.create_version(
    agent_name="demo-agent",
    version="1.0",
    model="gpt-4o-mini",
    instructions="You are a helpful assistant.",
    tools=[]
)
```

#### Error Handling with Streaming

```python
try:
    response = client.responses.create(...)
    for event in response:
        # Process events
        pass
except Exception as e:
    print(f"Error during streaming: {e}")
```

### Testing and Validation

All scripts were tested after migration:

- ✅ Real-time output display working
- ✅ No streaming errors
- ✅ Agent responses accurate
- ✅ External integrations (QR codes, blob storage) functional
- ✅ Tracing spans properly created

### Benefits of Streaming Migration

1. **Real-Time User Experience**: Users see responses as they're generated
2. **Improved Responsiveness**: No waiting for complete responses
3. **Better Error Visibility**: Issues appear immediately during generation
4. **Resource Efficiency**: Reduced memory usage for long responses
5. **Modern API Usage**: Aligns with latest Azure AI patterns

### Migration Checklist for Streaming

- [x] Add `stream=True` to all `responses.create()` calls
- [x] Replace batch output processing with event loops
- [x] Remove response attribute access during iteration
- [x] Update error handling for streaming context
- [x] Test real-time output display
- [x] Verify external integrations work with streaming
- [x] Clean up unused imports and add comments

### Lessons Learned

1. **Response Object Changes**: Streaming responses don't have the same attributes as synchronous responses
2. **Event-Driven Processing**: All output handling must be event-based
3. **Attribute Access Timing**: Response metadata only available after streaming completes
4. **Import Cleanup**: Remove legacy imports that are no longer needed
5. **Testing Importance**: Each script requires individual testing due to different input/output patterns

This migration ensures all demo scripts use modern streaming responses while maintaining their specific functionality (image processing, tracing, output formatting, etc.).
