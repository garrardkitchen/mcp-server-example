using EverythingServer.Resources;
using EverythingServer.Prompts; // Add this using directive for prompts

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<Azure.ResourceManager.ArmClient>(sp =>
{
    // Use DefaultAzureCredential for authentication
    return new Azure.ResourceManager.ArmClient(new Azure.Identity.DefaultAzureCredential());
});
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<GitLabTools>()
    .WithTools<AzureTool>() // Register AzureTool
    .WithTools<SensitiveDataExampleTool>()
    .WithResources<UserResources>()
    // .WithResources<DirectResourceType>()
    .WithResources<SimpleResourceType>()
    .WithPrompts<TextPrompts>(); // Register prompts

var app = builder.Build();

app.MapMcp();

await app.RunAsync();
