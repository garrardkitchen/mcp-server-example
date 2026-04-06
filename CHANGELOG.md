# Changelog

All notable changes to this project are documented here.

## [Unreleased] - 2026-04-06

### Changed
- All tools, prompts, and resources: rewrote `[Description]` attributes to be LLM-optimised — verb-first, succinct, front-loaded with action and output shape; added `[Description]` to all bare parameters across `AzureTools`, `GitLabTools`, `WhoIsTool`, `SensitiveDataExampleTool`, `TextPrompts`, `UserResources`, `KitchenApplianceResources`, and `SimpleResourceType`
- `SensitiveDataExampleTool.AAAEcho`: fixed description (previously read "Sets a secret for a username" — wrong tool)



### Added
- `ElicitationTools`: New `BrowseAzureResourcesAsync` tool — guided multi-step elicitation that presents available Azure subscriptions (single-select), then resource groups for the chosen subscription (multi-select) alongside a resource type filter (single-select dropdown populated from deployed types in the subscription, with an "All resource types" default), and returns all matching resources per resource group as JSON keyed by `ResourceType/Name`
- `ElicitationTools`: Inject `ArmClient` dependency to support Azure operations from elicitation flows

### Changed
- `ElicitationTools` → `BrowseAzureResourcesAsync`: Resource group and resource type selections combined into a single elicitation dialog; OData filter passed server-side to `GetGenericResourcesAsync` when a specific type is chosen

## [Unreleased] - 2026-04-04

### Fixed
- `ElicitationTools`: Replace unsafe dictionary indexer `Content["key"]` with `TryGetValue` on all four `Content` accesses — the indexer threw `KeyNotFoundException` when a field was absent from the elicitation response (lines 50–60, 82–84)

### Added
- `Resources/KitchenApplianceResources.cs`: New MCP resource exposing a kitchen appliance catalogue as JSON with 10 rows (`kitchen://appliances/all`, `kitchen://appliances/{id}`), including fields for id, name, category, powerWatts, priceGbp, brand, and hasDigitalControls
- `.github/copilot-instructions.md`: Repository instructions for future Copilot sessions

### Changed
- Upgrade `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` from `0.4.0-preview.3` to `1.2.0`
- `ElicitationTools`: Replace removed `ElicitRequestParams.EnumSchema` with `UntitledSingleSelectEnumSchema`; use new `IsAccepted` convenience property on `ElicitResult`
- `SimpleResourceType`: Update `BlobResourceContents` creation to use `FromBytes()` factory (SDK v1.2.0 changed `Blob` from `string` to `ReadOnlyMemory<byte>`)
- `SimpleResourceType`: Fix `requestContext.Params?.Uri` to `requestContext.Params.Uri` (Params is now non-nullable in v1.2.0)
- All tools, resources, and prompts: Add `IconSource` with Fluent UI Emoji SVG URLs to `[McpServerTool]`, `[McpServerResource]`, and `[McpServerPrompt]` attributes
- `README.md`: Update MCP Inspector connection instructions from SSE (`/sse`) to Streamable HTTP (`/`) following v1.2.0 breaking change
- `AzureTools`: Fix pre-existing nullable dereference warning in `GetResourcePropertiesAsync`

## [0.1.0] - Initial release

- Initial MCP server implementation with Azure, GitLab, elicitation, sensitive data, and WhoIs tools
- User and template resource examples
- Text prompt examples (ReverseWord, OneSentenceSummary, SummaryBenefitsAndReferences)
- Multi-stage Dockerfile and GitHub Actions CI pipeline
