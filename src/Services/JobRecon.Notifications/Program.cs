using JobRecon.Notifications.Endpoints;
using JobRecon.Notifications.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddNotificationsServices(builder.Configuration);
builder.Services.AddNotificationsHangfire(builder.Configuration);
builder.Services.AddNotificationsAuthentication(builder.Configuration);

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "JobRecon Notifications API";
    config.Version = "v1";
    config.Description = "Notification and alert service for job matches";
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure pipeline
app.ConfigurePipeline();

// Map endpoints
app.MapNotificationEndpoints();
app.MapPreferenceEndpoints();

// Run migrations and configure jobs
await app.MigrateDatabaseAsync();
app.ConfigureRecurringJobs();

app.Run();
