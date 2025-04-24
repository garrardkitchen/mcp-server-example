# MCP Server Example

This project demonstrates a basic implementation of an MCP (Model Context Protocol) server using .NET. It provides endpoints for interacting with MCP clients and includes tools for testing and debugging, such as the MCP Inspector. The example also shows how to integrate with external services like GitLab using user secrets for configuration.

## How to run

>[!NOTE]
> To run the GitLabTool (SearchGroupsAsync), follow the instructions [here](#user-secrets)

```bash
dotnet build
dotnet run
```

## How to debug/test

If you're not receiving the responses you expect, or if you want to test and interact with your MCP server(s) outside of the VSCode environment, you can use the MCP Inspector. This tool provides a user-friendly interface for testing and debugging your MCP servers. [Access the MCP Inspector source code here](https://github.com/modelcontextprotocol/inspector).

To install the inspector, enter this into your terminal:

```bash
npx @modelcontextprotocol/inspector dotnet run
```

This then starts the MCP Inspector.  Click on the HTTP URL to access the Inspector:

```bash
Starting MCP inspector...
⚙️ Proxy server listening on port 6277
🔍 MCP Inspector is up and running at http://127.0.0.1:6274 🚀
New SSE connection
```

## User Secrets

If the project is missing a `<UserSecretsId>` then run this to setup user secrets for this project:

```
dotnet user-secrets init
```

GitLab PAT

```bash
dotnet user-secrets set "GitLab:Token" "<your-token-value>"
```

GitLab Domain

```bash
dotnet user-secrets set "GitLab:domain" "<your-domain-value>"
```