using EverythingServer.Middleware;
using EverythingServer.Resources;
using EverythingServer.Prompts;
using EverythingServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<Azure.ResourceManager.ArmClient>(sp =>
{
    var isDevelopment = builder.Environment.IsDevelopment();
    var credential = new Azure.Identity.DefaultAzureCredential(
        new Azure.Identity.DefaultAzureCredentialOptions
        {
            // Disable IMDS probe in dev — it hangs the debugger waiting for a timeout
            ExcludeManagedIdentityCredential = isDevelopment,
        });
    return new Azure.ResourceManager.ArmClient(credential);
});
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<GitLabTools>()
    .WithTools<AzureTool>() // Register AzureTool
    .WithTools<SensitiveDataExampleTool>()
    .WithTools<ElicitationTools>()
    .WithTools<WhoIsTool>()
    .WithResources<UserResources>()
    // .WithResources<DirectResourceType>()
    .WithResources<SimpleResourceType>()
    .WithResources<KitchenApplianceResources>()
    .WithPrompts<TextPrompts>();

var app = builder.Build();

app.UseMiddleware<UserAgentLoggingMiddleware>();
app.MapMcp();

await app.RunAsync();
