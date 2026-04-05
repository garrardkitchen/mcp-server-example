using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace EverythingServer.Resources;

[McpServerResourceType]
public class SimpleResourceType
{
    [McpServerResource(UriTemplate = "test://direct/text/resource", Name = "Direct Text Resource", MimeType = "text/plain", IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Page%20facing%20up/Flat/page_facing_up_flat.svg")]
    [Description("A direct text resource")]
    public static string DirectTextResource() => "This is a direct resource";

    [McpServerResource(UriTemplate = "test://template/resource/{id}", Name = "Template Resource", IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Page%20facing%20up/Flat/page_facing_up_flat.svg")]
    [Description("A template resource with a numeric ID")]
    public static ResourceContents TemplateResource(RequestContext<ReadResourceRequestParams> requestContext, int id)
    {
        int index = id - 1;
        if ((uint)index >= ResourceGenerator.Resources.Count)
        {
            throw new NotSupportedException($"Unknown resource: {requestContext.Params.Uri}");
        }

        var resource = ResourceGenerator.Resources[index];
        return resource.MimeType == "text/plain" ?
            new TextResourceContents
            {
                Text = resource.Description!,
                MimeType = resource.MimeType,
                Uri = resource.Uri,
            } :
            BlobResourceContents.FromBytes(
                Convert.FromBase64String(resource.Description!),
                resource.Uri,
                resource.MimeType);
    }
}