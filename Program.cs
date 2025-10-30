using EverythingServer.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddSingleton<Azure.ResourceManager.ArmClient>(sp =>
{
    // Use DefaultAzureCredential for authentication
    return new Azure.ResourceManager.ArmClient(new Azure.Identity.DefaultAzureCredential());
});
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<GitLabTools>()
    .WithTools<WhoIsTool>()
    .WithTools<AzureTool>() // Register AzureTool
    .WithResources<UserResources>()
    // .WithResources<DirectResourceType>()
    .WithResources<SimpleResourceType>();

var app = builder.Build();

app.MapMcp();

await app.RunAsync();
