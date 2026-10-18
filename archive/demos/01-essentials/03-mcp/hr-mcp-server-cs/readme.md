## HR MCP Server

### Run the server

```powershell
dotnet run
```

### Connect with MCP Inspector

1. Start the server (see above).
2. Launch the inspector with the provided config:

```powershell
npx @modelcontextprotocol/inspector --config inspector.config.json --server hr-mcp
```

The config at `inspector.config.json` tells the inspector to use the HR MCP server's Streamable HTTP base URL `http://localhost:47002`, matching the endpoint that `app.MapMcp()` exposes. This satisfies the newer CLI requirement that `--server` reference an entry in a config file.

#### Remote (Azure) deployment

```powershell
npx @modelcontextprotocol/inspector --config inspector.config.json --server hr-mcp-azure
```

This reuses the same inspector config but selects the `hr-mcp-azure-dev` entry, which points at `https://hr-mcp-server-dev.azurewebsites.net`. Make sure the Azure app is running and reachable before launching the inspector.

> **Heads-up:** Updating `inspector.config.json` is a local workflow change only—you don’t need to republish the Azure App Service after editing this file. Republish the .NET app itself only when the server code or configuration that lives in the deployed artifact changes.

## Dev Tunnel Instructions for HR MCP Server

### Overview

This document provides complete instructions for setting up and using dev tunnels with the HR MCP Server. Dev tunnels allow you to expose your locally running MCP server to the internet via a secure public URL, making it accessible for testing, sharing, and integration purposes.

### Basic Setup Commands

For a quick and simple setup, use these four basic commands:

#### 1. Login to DevTunnel

```bash
devtunnel user login
```

**What it does:** Authenticates you with the DevTunnel service using your Microsoft account. This is required before you can create or manage tunnels.

#### 2. Create a Tunnel

```bash
devtunnel create hr-mcp -a --host-header unchanged
```

**What it does:**

- Creates a new tunnel named `hr-mcp`
- `-a` flag makes the tunnel accessible to anyone (anonymous access)
- `--host-header unchanged` preserves the original host header, which is important for applications that depend on specific host values

#### 3. Create a Port Mapping

```bash
devtunnel port create hr-mcp -p 47002
```

**What it does:** Creates a port mapping for the `hr-mcp` tunnel, forwarding traffic to local port `47002` where the HR MCP Server is running.

#### 4. Start the Tunnel

```bash
devtunnel host hr-mcp
```

**What it does:** Starts hosting the `hr-mcp` tunnel, making your local application accessible via a public URL. This command will display the public URL that you can use to access your HR MCP Server from anywhere.

> **Note:** Keep the terminal window with `devtunnel host hr-mcp` open while you need the tunnel active. Make sure your HR MCP Server is running on port 47002 before starting the tunnel.
