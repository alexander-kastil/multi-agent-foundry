# Build a Data Analyst, Product Expert and Web Scout

A plain model cannot open your spreadsheet, does not know your product manual and has no idea what changed yesterday. In this demo you give three prompt agents one built-in tool each, then merge two of them into a single agent. You finish with a downloaded chart, a cited answer from your own document and a cited answer from the web.

## Prerequisites

- A Microsoft Foundry project with a `gpt-5.4` deployment and `az login` completed.
- The `Foundry User` role on the project.
- Python 3.12 or later. Web Search sends the query outside the Azure compliance boundary, so do not run this against regulated data.

## Run it

```bash
cd demos/03-agent-service/02-builtin-tools/builtin-tools-py
python -m venv .venv
.venv/Scripts/pip install -r requirements.txt
cp .env.example .env
.venv/Scripts/python builtin_tools.py
```

Set `PROJECT_ENDPOINT` in `.env` first. The `assets` folder holds `quarterly_results.csv` (company results by sector) and `product-info.md` (a product sheet with warranty and return terms).

## Step 1: Analyze a CSV with Code Interpreter

The agent receives the uploaded file inside its sandbox, writes Python and saves a PNG. The `container_file_citation` annotation on the last message tells you where the file lives.

```python
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
message = response.output[-1]
citation = [a for a in message.content[-1].annotations if a.type == "container_file_citation"][-1]
content = openai.containers.files.content.retrieve(file_id=citation.file_id, container_id=citation.container_id)
```

Expected output:

```text
== Step 1: Code Interpreter
output item: code_interpreter_call
output item: code_interpreter_call
output item: code_interpreter_call
output item: message
Downloaded transportation_operating_profit_bar_chart.png (114706 bytes)
```

The `code_interpreter_call` items are the proof that code ran; the model iterated three times before it answered. The script deletes the PNG at the end, so open it before the run finishes or remove the last line while you explore.

## Step 2: Ground an agent in a document with File Search

Here the document goes into a vector store, and the agent searches it instead of guessing. `upload_and_poll` returns only after indexing has finished.

```python
store = openai.vector_stores.create(name="m3-product-docs")
with open(os.path.join(here, "assets", "product-info.md"), "rb") as f:
    doc = openai.vector_stores.files.upload_and_poll(vector_store_id=store.id, file=f)
support = project.agents.create_version(
    agent_name="m3-product-agent",
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="Answer only from the product documents. Cite the file you used.",
        tools=[FileSearchTool(vector_store_ids=[store.id])],
    ),
)
response = ask(support, "How long is the warranty and what is the return policy?")
```

Expected output:

```text
== Step 2: File Search
Vector store: vs_XGjSzPTuvXEYcPJqOPcEgbgd
The warranty is a two-year limited warranty on all electronic components, and the return policy is a 30-day return policy with no questions asked 
output item: file_search_call
output item: message
citation: file_citation product-info.md
```

The warranty and return terms exist only in `product-info.md`, so the `file_citation` is the evidence the answer came from your document.

## Step 3: Look up a current fact with Web Search

Web Search needs no Bing resource. The tool class takes no arguments for general search.

```python
scout = project.agents.create_version(
    agent_name="m3-web-agent",
    definition=PromptAgentDefinition(
        model=MODEL,
        instructions="Use web search for current facts and cite your sources.",
        tools=[WebSearchTool()],
    ),
)
response = ask(scout, "What is the latest stable version of the azure-ai-projects Python package on PyPI?")
for annotation in response.output[-1].content[-1].annotations:
    if annotation.type == "url_citation":
        print(f"citation: {annotation.url}")
```

Expected output:

```text
== Step 3: Web Search
The latest **stable** version of the `azure-ai-projects` Python package on PyPI appears to be **2.8.0** on the PyPI project page. ...
output item: web_search_call
output item: message
citation: https://pypi.org/project/azure-ai-projects/
citation: https://pypistats.org/packages/azure-ai-projects
```

The version in your run depends on the search index at that moment and can lag behind PyPI. The `url_citation` is what matters: it lets you check the source.

## Step 4: Combine two tools in one agent

An agent version can hold several tools. This agent finds the warranty length in the document and computes the day count with Python.

```python
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
print("tools used: " + ", ".join(i.type for i in response.output))
```

Expected output:

```text
== Step 4: Combine tools in one agent
The glasses have a two-year limited warranty .

Using Python: 2 × 365 = 730

So, the warranty lasts 730 days.
tools used: file_search_call, code_interpreter_call, message
```

The `tools used` line shows the model chose both tools in sequence.

## Run the C# version

The C# project covers Code Interpreter, File Search and Web Search with the same agents. `ResponseTool.CreateCodeInterpreterTool`, `CreateFileSearchTool` and `CreateWebSearchTool` build the tools, and the file and vector store clients come from `ProjectOpenAIClient`.

```bash
cd demos/03-agent-service/02-builtin-tools/builtin-tools-cs
cp .env.example .env
dotnet run
```

```csharp
ProjectsAgentVersion support = MakeAgent(
    "m3-doc-support",
    "Answer only from the product documents. Say when the documents do not contain the answer.",
    ResponseTool.CreateFileSearchTool(vectorStoreIds: [store.Id]));
ResponseResult answer = Ask(support, "What is the battery capacity in mAh?");
```

Expected output:

```text
== Step 2: File Search answers from an uploaded document
vector store vs_liKkok9zRTYQ7OTIgybbFOwO ready
Created m3-doc-support:1
user: What is the battery capacity in mAh?
tool calls: FileSearchCallResponseItem
agent: The product document does not specify the battery capacity in mAh. It only mentions "wireless charging with extended battery life," without giving a numeric capacity
```

The agent refuses to guess because the instructions tell it to answer only from the documents.

## Verify

Each of the four steps printed a tool call item before its `message`: `code_interpreter_call`, `file_search_call`, `web_search_call` and both of the first two together. Step 2 carries a `file_citation` for `product-info.md` and Step 3 a `url_citation`.

## Cleanup

The script deletes all four agent versions, the vector store, the uploaded CSV and the downloaded chart. To check that nothing is left, list the agents in your project:

```python
for agent in project.agents.list():
    print(agent.name)
```
