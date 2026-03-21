using JobRecon.Matching.Endpoints;
using JobRecon.Matching.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddMatchingServices(builder.Configuration);
builder.Services.AddMatchingAuthentication(builder.Configuration);

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "JobRecon Matching API";
    config.Version = "v1";
    config.Description = "Job matching and recommendations service";
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi();
}

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapMatchingEndpoints();
app.MapHealthChecks("/health");

app.Run();
