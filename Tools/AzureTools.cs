using System.ComponentModel;
using ModelContextProtocol.Server;
using Azure.Identity;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;

[McpServerToolType]
public class AzureTool
{
    private readonly ArmClient _client;

    public AzureTool(ArmClient client)
    {
        _client = client;
    }

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
}
