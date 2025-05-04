using System.ComponentModel;
using ModelContextProtocol.Server;
using Azure.Identity;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Compute;

[McpServerToolType]
public class AzureTool
{
    [McpServerTool, Description("Retrieves a list of azure subscriptions and their ID")]
    public async Task<Dictionary<string, string>> GetAzureSubscriptions()
    {
        var result = new Dictionary<string, string>();
        var credential = new DefaultAzureCredential();
        var client = new ArmClient(credential);

        await foreach (var subscription in client.GetSubscriptions())
        {
            result[subscription.Data.DisplayName] = subscription.Data.SubscriptionId;
        }

        return result;
    }

    [McpServerTool, Description("Retrieves a list of azure resource groups for a subscription")]
    public async Task<Dictionary<string, string>> GetListOfResourceGroups(string subscriptionId)
    {
        var result = new Dictionary<string, string>();
        var credential = new DefaultAzureCredential();
        var client = new ArmClient(credential);

        var subscriptionResource =
            client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));

        subscriptionResource.GetResourceGroups();
        await foreach (var resourceGroup in subscriptionResource.GetResourceGroups())
        {
            result[resourceGroup.Data.Name] = resourceGroup.Data.Id.ToString();
        }

        return result;
    }

    [McpServerTool,
     Description(
         "Retrieves all virtual machine names and their tags, from a subscription, where the virtual machine tags has a tag that matches a tag key")]
    public async Task<Dictionary<string, IDictionary<string, string>>> GetVirtualMachinesMatchTagKey(
        string subscriptionId, string tagKey)
    {
        var result = new Dictionary<string, IDictionary<string, string>>();
        var credential = new DefaultAzureCredential();
        var client = new ArmClient(credential);

        var subscription = client.GetSubscriptionResource(new ResourceIdentifier($"/subscriptions/{subscriptionId}"));
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