# Azure AI Agent Service - Composite Migration Guide

## Knowledge Tools Samples: Legacy API → Microsoft Foundry API

**Last Updated:** November 27, 2025

---

## Executive Summary

This document provides a comprehensive migration strategy for all samples in `demos/02-agent-service/02-knowledge-tools/agent-knowledge-tools-py/` from the legacy `AgentsClient` API to the new Microsoft Foundry API (`AIProjectClient`).

### Current Migration Status

| Status                       | Count | Samples                                           |
| ---------------------------- | ----- | ------------------------------------------------- |
| ✅ **Can Migrate**           | 2     | `agents-foundry-iq.py` (done), `agents-mcp.py`    |
| ⚠️ **Alternative Available** | 2     | `agents-ai-search-rag.py`, `agents-sharepoint.py` |
| 🚫 **Blocked**               | 8     | All other samples                                 |

### Key Findings

1. **Limited Tool Support:** The new API currently only supports `MCPTool`
2. **Foundry IQ as Alternative:** Can replace Azure AI Search and potentially SharePoint
3. **Most Tools Unavailable:** File Search, Code Interpreter, Functions, Browser Automation, Computer Use, Bing Grounding, and OpenAPI tools are not yet in the new API
4. **Hybrid Approach Recommended:** Maintain both APIs until full parity is achieved

---

## Tool Compatibility Matrix

| Tool                   | Legacy API                 | New API      | Status      | Migration Path                |
| ---------------------- | -------------------------- | ------------ | ----------- | ----------------------------- |
| **Basic Agent**        | ✅                         | ✅           | **READY**   | Direct migration              |
| **MCP**                | ✅ `McpTool`               | ✅ `MCPTool` | **READY**   | Update capitalization         |
| **Foundry IQ**         | ❌                         | ✅ `MCPTool` | **NEW**     | Use MCP with KB endpoint      |
| **File Search**        | ✅ `FileSearchTool`        | ❌           | **BLOCKED** | Wait or use Foundry IQ        |
| **Azure AI Search**    | ✅ `AzureAISearchTool`     | ❌           | **BLOCKED** | Use Foundry IQ alternative    |
| **Bing Grounding**     | ✅ `BingGroundingTool`     | ❌           | **BLOCKED** | Wait for tool support         |
| **SharePoint**         | ✅ `SharepointTool`        | ❌           | **BLOCKED** | Use Foundry IQ with SP source |
| **Code Interpreter**   | ✅ `CodeInterpreterTool`   | ❌           | **BLOCKED** | Wait for tool support         |
| **Function Calling**   | ✅ `FunctionTool`          | ❌           | **BLOCKED** | Wait or MCP wrapper           |
| **Browser Automation** | ✅ `BrowserAutomationTool` | ❌           | **BLOCKED** | Wait for tool support         |
| **Computer Use**       | ✅ `ComputerUseTool`       | ❌           | **BLOCKED** | Wait for tool support         |
| **OpenAPI/REST**       | ✅ `OpenApiTool`           | ❌           | **BLOCKED** | Wait for tool support         |

---

## Individual Migration Guides

Each sample has a detailed migration guide:

1. **agents-file-search-MIGRATION.md** - File Search tool (BLOCKED)
2. **agents-ai-search-rag-MIGRATION.md** - Azure AI Search RAG (Alternative: Foundry IQ)
3. **agents-bing-grounding-MIGRATION.md** - Bing Grounding (BLOCKED)
4. **agents-sharepoint-MIGRATION.md** - SharePoint (Alternative: Foundry IQ)
5. **agents-code-interpreter-MIGRATION.md** - Code Interpreter (BLOCKED)
6. **agents-function-calling-MIGRATION.md** - Function Calling (BLOCKED)
7. **agents-azfunction-MIGRATION.md** - Azure Functions (BLOCKED)
8. **agents-browser-automation-MIGRATION.md** - Browser Automation (BLOCKED)
9. **agents-computer-use-MIGRATION.md** - Computer Use (BLOCKED)
10. **agents-rest-calling-MIGRATION.md** - REST/OpenAPI (BLOCKED)
11. **agents-mcp-MIGRATION.md** - MCP (READY TO MIGRATE)

---

## Implementation Roadmap

### Phase 1: Immediate Actions (This Week)

**✅ COMPLETED:**

- Individual migration guides created
- Composite migration guide created
- Tool compatibility matrix documented

**TODO:**

1. **Migrate agents-mcp.py**

   - Follow `agents-mcp-MIGRATION.md`
   - Update to use `MCPTool` (capitalized)
   - Replace thread/run with streaming responses
   - Test thoroughly

2. **Add Migration Notes to Blocked Samples**

   - Add header comment to each blocked sample file
   - Format:

   ```python
   # NOTE: This sample uses the legacy AgentsClient API because [ToolName]
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # See [filename]-MIGRATION.md for details and alternatives.
   ```

3. **Update Main Readme**
   - Add tool compatibility matrix
   - Link to migration guides
   - Document hybrid API approach

### Phase 2: Foundry IQ Migration (Optional)

If Foundry IQ infrastructure is available:

1. **Migrate agents-ai-search-rag.py**

   - Create knowledge base with insurance documents
   - Follow `agents-ai-search-rag-MIGRATION.md`
   - Keep original as reference

2. **Consider agents-sharepoint.py**
   - Add SharePoint knowledge source
   - Migrate if beneficial

### Phase 3: Ongoing Monitoring

- Monitor `azure-ai-projects` SDK releases
- Migrate samples as tools become available
- Update compatibility matrix

---

## Quick Start: Coding Agent Instructions

To pass this to the coding agent, use these instructions:

### For Migrating agents-mcp.py:

```
Migrate demos/02-agent-service/02-knowledge-tools/agent-knowledge-tools-py/agents-mcp.py to use the new Microsoft Foundry API.

Follow the migration guide in agents-mcp-MIGRATION.md:

1. Update imports:
   - from azure.ai.agents import AgentsClient → from azure.ai.projects import AIProjectClient
   - from azure.ai.agents.models import McpTool → from azure.ai.projects.models import PromptAgentDefinition, MCPTool

2. Update client initialization:
   - Create AIProjectClient instead of AgentsClient
   - Get openai_client from project_client

3. Update MCP tool:
   - McpTool → MCPTool (capitalize)
   - Add require_approval="never" parameter

4. Update agent creation:
   - Use create_version() with PromptAgentDefinition
   - Pass tools as list: tools=[mcp_tool]

5. Replace thread/run pattern with streaming responses:
   - Use openai_client.responses.create() with stream=True
   - Loop through events, print deltas
   - Handle response.completed event

6. Update cleanup:
   - Use delete_version() instead of delete_agent()
   - Update env var: DELETE_AGENT_ON_EXIT → DELETE

7. Remove UTF-8 encoding handling (not needed)

8. Test thoroughly and verify agent appears in Microsoft Foundry portal
```

### For Adding Migration Notes to Blocked Samples:

```
Add migration notes to the following files in demos/02-agent-service/02-knowledge-tools/agent-knowledge-tools-py/:

1. agents-file-search.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because FileSearchTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # See agents-file-search-MIGRATION.md for details and alternatives.

2. agents-ai-search-rag.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because AzureAISearchTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # For RAG with the new API, see agents-foundry-iq.py (Foundry IQ Knowledge Base).
   # See agents-ai-search-rag-MIGRATION.md for migration guidance.

3. agents-bing-grounding.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because BingGroundingTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # See agents-bing-grounding-MIGRATION.md for details.

4. agents-sharepoint.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because SharepointTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # For SharePoint search with the new API, see Foundry IQ with SharePoint knowledge source.
   # See agents-sharepoint-MIGRATION.md for migration guidance.

5. agents-code-interpreter.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because CodeInterpreterTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # See agents-code-interpreter-MIGRATION.md for details.

6. agents-function-calling.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because FunctionTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # See agents-function-calling-MIGRATION.md for details.

7. agents-azfunction.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because FunctionTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # See agents-azfunction-MIGRATION.md for details.

8. agents-browser-automation.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because BrowserAutomationTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # See agents-browser-automation-MIGRATION.md for details.

9. agents-computer-use.py - Add after imports:
   # NOTE: This sample uses the legacy AgentsClient API because ComputerUseTool
   # is not yet available in the new Microsoft Foundry API (AIProjectClient).
   # See agents-computer-use-MIGRATION.md for details.

10. agents-rest-calling.py - Add after imports:
    # NOTE: This sample uses the legacy AgentsClient API because OpenApiTool
    # is not yet available in the new Microsoft Foundry API (AIProjectClient).
    # See agents-rest-calling-MIGRATION.md for details.

Also update these samples to use the new env var convention (DELETE instead of DELETE_AGENT_ON_EXIT) and remove UTF-8 encoding handling where present.
```

---

## Success Criteria

### Phase 1 Complete When:

- ✅ All individual migration guides created
- ✅ Composite migration guide created
- ⏳ `agents-mcp.py` migrated and tested
- ⏳ All blocked samples have migration notes in file headers
- ⏳ Main readme updated with compatibility matrix

### Phase 2 Complete When:

- ⏳ Foundry IQ infrastructure created
- ⏳ At least one RAG sample using Foundry IQ
- ⏳ Comparison documentation available

---

## Key Takeaways for Coding Agent

1. **Only 2 samples can be migrated now:** `agents-foundry-iq.py` (already done) and `agents-mcp.py`

2. **8 samples are blocked** and should just get header notes explaining why they can't be migrated yet

3. **2 samples have alternatives** (`agents-ai-search-rag.py` and `agents-sharepoint.py`) via Foundry IQ, but require additional infrastructure setup

4. **Main migration pattern:**

   - `AgentsClient` → `AIProjectClient`
   - `create_agent()` → `create_version()` with `PromptAgentDefinition`
   - Thread/Run → Streaming Responses
   - `delete_agent()` → `delete_version()`
   - `DELETE_AGENT_ON_EXIT` → `DELETE`

5. **All migration guides are in `*-MIGRATION.md` files** in the same directory

---

**End of Composite Migration Guide**
