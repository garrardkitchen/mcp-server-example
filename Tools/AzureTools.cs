using System.ComponentModel;
using ModelContextProtocol.Server;
using Azure.Identity;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;

/// <summary>
/// Represents a tool for interacting with Microsoft Azure resources.
/// Provides methods to retrieve Azure subscriptions, resource groups,
/// and virtual machines based on specified criteria.
/// </summary>
[McpServerToolType]
public class AzureTool
{
    private readonly ArmClient _client;

    /// <summary>
    /// Represents a tool for interacting with Microsoft Azure resources.
    /// </summary>
    public AzureTool(ArmClient client)
    {
        _client = client;
    }

    /// <summary>
    /// Retrieves a dictionary containing the names and IDs of accessible Azure subscriptions.
    /// </summary>
    /// <returns>A dictionary where the key is the subscription display name, and the value is the corresponding subscription ID.</returns>
    [McpServerTool, Description("Retrieves a list of azure subscriptions and their ID")]
    public async Task<Dictionary<string, string>> GetAzureSubscriptionsAsync()
    {
        var result = new Dictionary<string, string>();
        await foreach (var subscription in _client.GetSubscriptions())
        {
            result[subscription.Data.DisplayName] = subscription.Data.SubscriptionId;
        }
        return result;
    }

    /// <summary>
    /// Retrieves a dictionary containing the names and IDs of resource groups within a specified Azure subscription.
    /// </summary>
    /// <param name="subscriptionId">The ID of the Azure subscription whose resource groups are to be retrieved.</param>
    /// <returns>A dictionary where the key is the resource group name, and the value is the corresponding resource group ID.</returns>
    [McpServerTool, Description("Retrieves a list of azure resource groups for a subscription")]
    public async Task<Dictionary<string, string>> GetListOfResourceGroupsAsync(string subscriptionId)
    {
        var result = new Dictionary<string, string>();
        var subscriptionResource =
            _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        await foreach (var resourceGroup in subscriptionResource.GetResourceGroups())
        {
            result[resourceGroup.Data.Name] = resourceGroup.Data.Id.ToString();
        }

        return result;
    }

    /// <summary>
    /// Retrieves a dictionary of virtual machine names and their associated tags within a specified Azure subscription.
    /// Only virtual machines that have a tag matching the specified tag key are included.
    /// </summary>
    /// <param name="subscriptionId">The ID of the Azure subscription to retrieve virtual machines from.</param>
    /// <param name="tagKey">The tag key to filter the virtual machines by.</param>
    /// <returns>A dictionary where the key is the virtual machine name, and the value is its corresponding tags.</returns>
    [McpServerTool,
     Description(
         "Retrieves all virtual machine names and their tags, from a subscription, where the virtual machine tags has a tag that matches a tag key")]
    public async Task<Dictionary<string, IDictionary<string, string>>> GetVirtualMachinesMatchTagKeyAsync(
        string subscriptionId, string tagKey)
    {
        var result = new Dictionary<string, IDictionary<string, string>>();
        var subscription = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        await foreach (var vm in subscription.GetVirtualMachinesAsync())
        {
            if (vm.Data.Tags != null &&
                vm.Data.Tags.TryGetValue(tagKey, out var value))
            {
                result[vm.Data.Name] = vm.Data.Tags;
            }
        }

        return result;
    }

    /// <summary>
    /// Retrieves all resources within a specified resource group.
    /// </summary>
    /// <param name="subscriptionId">The subscription ID where the resource group is located.</param>
    /// <param name="resourceGroupName">The name of the resource group to retrieve resources from.</param>
    /// <returns>A dictionary where the key is the resource name and the value is another dictionary containing metadata about the resource.</returns>
    [McpServerTool, Description("Retrieves all resources within a specified resource group")]
    public async Task<Dictionary<string, IDictionary<string, string>>> GetResourcesInResourceGroupAsync(string subscriptionId, string resourceGroupName)
    {
        var result = new Dictionary<string, IDictionary<string, string>> ();
        var subscription = _client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
        var resourceGroup = subscription.GetResourceGroup(resourceGroupName);

        await foreach (var resource in resourceGroup.Value.GetGenericResourcesAsync().AsPages())
        {
            foreach (var page in resource.Values)
            {
                result[page.Data.Name] = new Dictionary<string, string>
                {
                    ["ResourceType"] = page.Data.ResourceType.ToString(),
                    ["ResourceId"] = page.Data.Id.ToString(),
                };
                    
            }
        }

        return result;
    }

    /// <summary>
    /// Retrieves a dictionary of properties and metadata for a specific Azure resource based on its resource ID.
    /// </summary>
    /// <param name="resourceId">The Resource ID of the Azure resource for which properties and metadata are to be retrieved.</param>
    /// <returns>A dictionary containing key-value pairs representing the properties and metadata of the specified Azure resource.</returns>
    [McpServerTool,
     Description("Retrieves a dictionary of properties and other metadata for a specific azure resource id")]
    public async Task<Dictionary<string, string>> GetResourcePropertiesAsync(string resourceId)
    {
        var result = new Dictionary<string, string>();
        var resource = await _client.GetGenericResource(new ResourceIdentifier(resourceId)).GetAsync();

        // result[page.Data.Name] = page.Data.ResourceType.ToString();
        if (resource != null && resource.Value.Data != null)
        {
            var data = resource.Value.Data;
            // Add common properties
            result["Id"] = data.Id.ToString();
            result["Name"] = data.Name;
            result["Type"] = data.ResourceType.ToString();
            result["Location"] = data.Location;
            result["Kind"] = data.Kind;
            result["ManagedBy"] = data.ManagedBy;
            result["Tags"] = data.Tags != null ? string.Join(";", data.Tags.Select(kv => $"{kv.Key}={kv.Value}")) : "";

            // Add all additional properties from the Properties dictionary if available
            if (data.Properties != null)
            {
                var propertiesDictionary = data.Properties.ToObjectFromJson<Dictionary<string, object>>();
                foreach (var prop in propertiesDictionary)
                {
                    result[$"Property:{prop.Key}"] = prop.Value?.ToString() ?? "";
                }
            }
        }
        
        return result;
    }
}
