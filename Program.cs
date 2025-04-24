var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<GitLabTools>()
    .WithTools<WhoIsTool>();

var app = builder.Build();

app.MapMcp();

await app.RunAsync();