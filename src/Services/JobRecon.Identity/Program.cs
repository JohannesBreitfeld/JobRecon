using JobRecon.Identity.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIdentityServices(builder.Configuration);

var app = builder.Build();

app.ConfigurePipeline();
app.MapEndpoints();

await app.SeedDataAsync();

app.Run();
