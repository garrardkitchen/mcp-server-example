using System.ComponentModel;
using System.Text.Json;
using Azure.Core;
using Azure.ResourceManager;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EverythingServer.Tools;

[McpServerToolType]
public class ElicitationTools
{
    private readonly ILogger<ElicitationTools> _logger;
    private readonly ArmClient _armClient;
    private static readonly Random _random = Random.Shared;

    public ElicitationTools(ILogger<ElicitationTools> logger, ArmClient armClient)
    {
        _logger = logger;
        _armClient = armClient;
    }
    
    [McpServerTool(IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Game%20die/Flat/game_die_flat.svg")]
    [Description("Interactive number-guessing game. Prompts the user for range (0–10 or 11–20), a title, a date, and multi-select options, then asks for a numeric guess and reports win/loss. Demonstrates MCP elicitation schema types. #elicitation")]
    public async Task<string> GuessTheNumber(McpServer server, CancellationToken token)
    {
        
        _logger.LogInformation("GuessTheNumber tool invoked");
        // Ask if they want to play
        var playResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Do you want to play a game?",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["Options"] = new ElicitRequestParams.TitledSingleSelectEnumSchema
                    { 
                        OneOf = new List<ElicitRequestParams.EnumSchemaOption>
                        {
                            new() { Const = "0-10", Title = "Low range (0–10)" },
                            new() { Const = "11-20", Title = "High range (11–20)" }
                        }
                    },
                    ["Title"] = new ElicitRequestParams.StringSchema 
                    { 
                        MaxLength = 30, 
                        Description = "The title of the game" 
                    },
                    ["Date"] = new ElicitRequestParams.StringSchema 
                    { 
                        Description = "Enter a date: DD/MM/YYYY", 
                        Format = "date" 
                    },
                    ["Options2"] = new ElicitRequestParams.TitledMultiSelectEnumSchema
                    { 
                        Description = "Select multiple options",
                        Items = new ElicitRequestParams.TitledEnumItemsSchema
                        {
                            AnyOf = new List<ElicitRequestParams.EnumSchemaOption>
                            {
                                new() { Const = "opt1", Title = "Option 1" },
                                new() { Const = "opt2", Title = "Option 2" },
                                new() { Const = "opt3", Title = "Option 3" }
                            }
                        }
   
                    },
                    
                }
            }
        }, token);
        
        _logger.LogInformation("playResponse");

        if (!playResponse.IsAccepted)
            return "Maybe next time!";

        
        _logger.LogInformation("playResponse IsAccepted");
        
        // Get game parameters and ask for guess
        var title = playResponse.Content?.TryGetValue("Title", out var titleEl) == true
            ? titleEl.GetString() ?? "Guess the number"
            : "Guess the number";
        var useHighRange = playResponse.Content?.TryGetValue("Options", out var optionsEl) == true
            && optionsEl.GetString() == "11-20";
        var (min, max) = useHighRange ? (11, 20) : (0, 10);

        _logger.LogInformation("Game title: {Title}, Range: {Min}-{Max}", title, min, max);

        var guessResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = title,
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["Answer"] = new ElicitRequestParams.NumberSchema
                    {
                        Minimum = min,
                        Maximum = max,
                        Description = $"Enter a value between {min} and {max}"
                    }
                }
            }
        }, token);

        var guess = guessResponse.Content?.TryGetValue("Answer", out var answerEl) == true
            ? answerEl.GetInt32()
            : (int?)null;
        var correctAnswer = _random.Next(min, max + 1);

        _logger.LogInformation("Guess: {Guess}, Correct answer: {Answer}", guess, correctAnswer);

        return guess == correctAnswer 
            ? "You guessed correctly!" 
            : $"You guessed wrong! Correct answer was {correctAnswer}";
    }

    [McpServerTool(IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Cloud/Flat/cloud_flat.svg")]
    [Description("Guided Azure resource browser. Interactively selects a subscription (single-select), then resource groups (multi-select) and a resource type filter (dropdown). Returns matching resources as JSON keyed by 'ResourceType/Name'. #elicitation")]
    public async Task<string> BrowseAzureResourcesAsync(McpServer server, CancellationToken token)
    {
        // Step 1: Fetch subscriptions and present as single-select
        var subscriptions = new Dictionary<string, string>();
        try
        {
            await foreach (var sub in _armClient.GetSubscriptions().WithCancellation(token))
            {
                var displayName = sub.Data.DisplayName ?? sub.Data.SubscriptionId;
                subscriptions.TryAdd(displayName, sub.Data.SubscriptionId);
            }
        }
        catch (OperationCanceledException)
        {
            return "Operation cancelled.";
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to enumerate Azure subscriptions");
            return $"Failed to retrieve subscriptions: {ex.Message}";
        }

        if (subscriptions.Count == 0)
            return "No Azure subscriptions found.";

        var subOptions = subscriptions
            .Select(kvp => new ElicitRequestParams.EnumSchemaOption { Const = kvp.Value, Title = kvp.Key })
            .ToList();

        var subResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Select an Azure subscription:",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["Subscription"] = new ElicitRequestParams.TitledSingleSelectEnumSchema { OneOf = subOptions }
                }
            }
        }, token);

        if (!subResponse.IsAccepted)
            return "Cancelled.";

        var subscriptionId = subResponse.Content?.TryGetValue("Subscription", out var subEl) == true
            ? subEl.GetString()
            : null;

        if (string.IsNullOrEmpty(subscriptionId))
            return "No subscription selected.";

        _logger.LogInformation("Selected subscription: {SubscriptionId}", subscriptionId);

        // Step 2: Fetch resource groups and distinct deployed resource types, then present together
        var subscriptionResource = _armClient.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        var rgNames = new List<string>();
        var resourceTypes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await foreach (var rg in subscriptionResource.GetResourceGroups().WithCancellation(token))
                rgNames.Add(rg.Data.Name);

            await foreach (var resource in subscriptionResource.GetGenericResourcesAsync(cancellationToken: token))
                resourceTypes.Add(resource.Data.ResourceType.ToString());
        }
        catch (OperationCanceledException)
        {
            return "Operation cancelled.";
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Failed to enumerate resource groups or types for subscription {SubscriptionId}", subscriptionId);
            return $"Failed to retrieve resource groups or types: {ex.Message}";
        }

        if (rgNames.Count == 0)
            return $"No resource groups found in subscription {subscriptionId}.";

        var rgOptions = rgNames
            .Select(name => new ElicitRequestParams.EnumSchemaOption { Const = name, Title = name })
            .ToList();

        var typeOptions = new List<ElicitRequestParams.EnumSchemaOption>
        {
            new() { Const = "ALL", Title = "All resource types" }
        };
        typeOptions.AddRange(resourceTypes.Select(t => new ElicitRequestParams.EnumSchemaOption { Const = t, Title = t }));

        var selectionResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Select an optional resource type and resource groups filter:",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["ResourceType"] = new ElicitRequestParams.TitledSingleSelectEnumSchema
                    {
                        OneOf = typeOptions
                    },
                    ["ResourceGroups"] = new ElicitRequestParams.TitledMultiSelectEnumSchema
                    {
                        Description = "Select the resource groups you want to inspect",
                        Items = new ElicitRequestParams.TitledEnumItemsSchema { AnyOf = rgOptions }
                    }
                }
            }
        }, token);

        if (!selectionResponse.IsAccepted)
            return "Cancelled.";

        var selectedRgs = new List<string>();
        if (selectionResponse.Content?.TryGetValue("ResourceGroups", out var rgsEl) == true)
        {
            if (rgsEl.ValueKind == JsonValueKind.Array)
                selectedRgs = rgsEl.EnumerateArray().Select(e => e.GetString()).OfType<string>().ToList();
            else if (rgsEl.ValueKind == JsonValueKind.String && rgsEl.GetString() is { } single)
                selectedRgs = [single];
        }

        if (selectedRgs.Count == 0)
            return "No resource groups selected.";

        var selectedType = selectionResponse.Content?.TryGetValue("ResourceType", out var typeEl) == true
            ? typeEl.GetString() ?? "ALL"
            : "ALL";

        _logger.LogInformation("Selected resource groups: {ResourceGroups}, type filter: {ResourceType}",
            string.Join(", ", selectedRgs), selectedType);

        // Build OData filter for SDK — avoids fetching unwanted resource types server-side
        var odataFilter = selectedType.Equals("ALL", StringComparison.OrdinalIgnoreCase)
            ? null
            : $"resourceType eq '{selectedType}'";

        // Step 3: Retrieve resources for each selected resource group, applying the type filter
        // Key is "ResourceType/Name" to avoid collisions when multiple resource types share a name
        var allResults = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        foreach (var rgName in selectedRgs)
        {
            var resources = new Dictionary<string, Dictionary<string, string>>();
            try
            {
                var resourceGroup = subscriptionResource.GetResourceGroup(rgName);
                await foreach (var page in resourceGroup.Value.GetGenericResourcesAsync(filter: odataFilter, cancellationToken: token).AsPages())
                {
                    foreach (var resource in page.Values)
                    {
                        var key = $"{resource.Data.ResourceType.Type}/{resource.Data.Name}";
                        resources[key] = new Dictionary<string, string>
                        {
                            ["ResourceType"] = resource.Data.ResourceType.ToString(),
                            ["ResourceId"] = resource.Data.Id.ToString(),
                        };
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return "Operation cancelled.";
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to enumerate resources in resource group {ResourceGroup}", rgName);
                resources["_error"] = new Dictionary<string, string> { ["Message"] = ex.Message };
            }

            allResults[rgName] = resources;
        }

        _logger.LogInformation("BrowseAzureResourcesAsync completed for {Count} resource group(s) with type filter '{ResourceType}'",
            allResults.Count, selectedType);
        return JsonSerializer.Serialize(allResults, new JsonSerializerOptions { WriteIndented = true });
    }
}