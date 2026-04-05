# Copilot Instructions

## Build & Run

```bash
dotnet build
dotnet run                  # Starts MCP server on http://localhost:5168
```

Debug interactively with the MCP Inspector:
```bash
npx @modelcontextprotocol/inspector dotnet run
```

Connect the inspector using **Transport Type: Streamable HTTP** to `http://localhost:5168/` (root endpoint — not `/sse`). As of SDK v1.2.0, legacy SSE is disabled by default.

No test project exists — use the MCP Inspector for integration testing.

## Architecture

This is an **ASP.NET Core MCP server** using `ModelContextProtocol.AspNetCore` (v1.2.0). The three MCP primitives map to three folders:

| Folder | MCP Primitive | Purpose |
|--------|--------------|---------|
| `Tools/` | Tool | Actions the LLM can invoke |
| `Resources/` | Resource | Data/content the LLM can read |
| `Prompts/` | Prompt | Instruction templates |

`Program.cs` wires everything together via a fluent builder:

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<GitLabTools>()
    .WithResources<UserResources>()
    .WithPrompts<TextPrompts>();

app.MapMcp();
```

`ResourceGenerator.cs` pre-generates 100 mock resources at startup (alternating plaintext/base64) for the `test://template/resource/{id}` URI template.

## Adding New Tools / Resources / Prompts

**Tool:**
1. Create a class in `Tools/` decorated with `[McpServerToolType]` and `[Description("...")]`
2. Add methods decorated with `[McpServerTool]` and `[Description("...")]` on parameters
3. Register in `Program.cs`: `.WithTools<YourToolClass>()`

**Resource:**
1. Create a class in `Resources/` with `[McpServerResourceType]`
2. Use `[McpServerResource(UriTemplate = "scheme://path/{param}")]` for template resources
3. Register: `.WithResources<YourResourceClass>()`

**Prompt:**
1. Create a class in `Prompts/` with `[McpServerPromptType]`
2. Add methods with `[McpServerPrompt]`
3. Register: `.WithPrompts<YourPromptClass>()`

## Key Conventions

- **Icons**: Add `IconSource = "https://..."` to `[McpServerTool]`, `[McpServerResource]`, and `[McpServerPrompt]` attributes — use Fluent UI Emoji SVG URLs (e.g. `https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Cloud/Flat/cloud_flat.svg`)
- **Naming**: `[Feature]Tools`, `[Feature]Resources`, `[Feature]Prompts`
- **Async**: Async methods use the `Async` suffix
- **DI**: Inject `ILogger<T>`, `IConfiguration`, `ArmClient`, or `McpServer` via constructor
- **YAML**: `UserResources` serializes data as YAML using `YamlDotNet` — follow this pattern for structured resource output
- **Secret masking**: Use `GitLabExtensions.MaskValue()` (shows first 4 chars) when returning sensitive values in tool responses
- **Git operations**: `GitLabTools` shells out via `ProcessStartInfo` for git commands — follow the same pattern for any CLI subprocess calls

## Configuration & Secrets

GitLab credentials are stored in **user secrets** (never in appsettings):

```bash
dotnet user-secrets set "GitLab:Token" "<PAT>"
dotnet user-secrets set "GitLab:Domain" "<gitlab-domain-url>"
```

Access via `ConfigurationExtensions`:
```csharp
var token = configuration.GetGitLabToken();
var domain = configuration.GetGitLabDomain();
```

Azure auth uses `DefaultAzureCredential` — no explicit credentials in config.

## Docker & CI

```bash
docker build -t mcp-server-example .   # Multi-stage build, exposes port 8080
```

The CI pipeline (`docker-build.yml`) is **generated** by `setup-workflow.sh` — edit the shell script, not the workflow file directly. It builds multi-platform images (`linux/amd64`, `linux/arm64`) and tags as `VERSION` on `main` or `VERSION-preview-TIMESTAMP` on feature branches.

Secrets required in the GitHub repo: `DOCKER_USERNAME`, `DOCKER_PASSWORD`.
