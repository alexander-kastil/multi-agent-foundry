import logging

import azure.functions as func

app = func.FunctionApp()

BLOB_PATH = "snippets/{mcptoolargs.snippetname}.json"


@app.mcp_tool()
def hello_mcp() -> str:
    """Says hello from an MCP server hosted on Azure Functions."""
    return "Hello I am MCPTool!"


@app.mcp_tool()
@app.mcp_tool_property(arg_name="snippetname", description="The name of the snippet.")
@app.blob_input(arg_name="file", connection="AzureWebJobsStorage", path=BLOB_PATH)
def get_snippet(file: func.InputStream, snippetname: str) -> str:
    """Retrieves a snippet by name from blob storage."""
    content = file.read().decode("utf-8")
    logging.info("Retrieved snippet %s", snippetname)
    return content


@app.mcp_tool()
@app.mcp_tool_property(arg_name="snippetname", description="The name of the snippet.")
@app.mcp_tool_property(arg_name="snippet", description="The content of the snippet.")
@app.blob_output(arg_name="file", connection="AzureWebJobsStorage", path=BLOB_PATH)
def save_snippet(file: func.Out[str], snippetname: str, snippet: str) -> str:
    """Saves a snippet under a name into blob storage."""
    file.set(snippet)
    logging.info("Saved snippet %s", snippetname)
    return f"Snippet '{snippetname}' saved successfully"
