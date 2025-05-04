var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<GitLabTools>()
    .WithTools<WhoIsTool>()
    .WithTools<AzureTool>(); // Register AzureTool

var app = builder.Build();

app.MapMcp();

await app.RunAsync();
