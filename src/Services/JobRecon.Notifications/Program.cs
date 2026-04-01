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
builder.Services.AddHealthChecks()
    .AddDbContextCheck<JobRecon.Notifications.Infrastructure.NotificationsDbContext>("database");

var app = builder.Build();

// Configure pipeline
app.ConfigurePipeline();

// Map endpoints
app.MapNotificationEndpoints();
app.MapPreferenceEndpoints();

// Run migrations, purge stale jobs, and configure recurring jobs
await app.MigrateDatabaseAsync();
app.PurgeStaleHangfireJobs();
app.ConfigureRecurringJobs();

app.Run();
