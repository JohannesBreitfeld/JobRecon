using JobRecon.Profile.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProfileServices(builder.Configuration);
builder.Services.AddProfileAuthentication(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "JobRecon Profile API";
    config.Version = "v1";
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.ConfigurePipeline();
app.MapEndpoints();

await app.MigrateDatabaseAsync();

app.Run();
