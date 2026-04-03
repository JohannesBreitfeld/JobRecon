using Hangfire;
using Hangfire.Dashboard;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Infrastructure;
using JobRecon.Jobs.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Jobs.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()]
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready");

        return app;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        await dbContext.Database.MigrateAsync();

        // Seed localities if empty
        if (!await dbContext.Localities.AnyAsync())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobsDbContext>>();
            var seedFile = Path.Combine(AppContext.BaseDirectory, "Data", "se-localities.txt");

            if (File.Exists(seedFile))
            {
                logger.LogInformation("Seeding localities from {FilePath}", seedFile);
                var importService = scope.ServiceProvider.GetRequiredService<ILocalityImportService>();
                var count = await importService.ImportFromGeoNamesFileAsync(seedFile);
                logger.LogInformation("Seeded {Count} localities", count);
            }
            else
            {
                logger.LogWarning("Locality seed file not found at {FilePath}. Run import manually.", seedFile);
            }
        }
    }

    public static void ConfigureRecurringJobs(this WebApplication app)
    {
        // Fetch jobs every hour (daily files are published once per day, but check hourly to pick up new files quickly)
        RecurringJob.AddOrUpdate<IJobFetcherService>(
            "fetch-all-jobs",
            service => service.FetchAllJobsAsync(CancellationToken.None),
            "0 * * * *"); // Every hour at minute 0

        // Enrich pending jobs every 15 minutes
        RecurringJob.AddOrUpdate<IJobEnrichmentService>(
            "enrich-pending-jobs",
            service => service.EnrichPendingJobsAsync(50, CancellationToken.None),
            "*/15 * * * *"); // Every 15 minutes

        // Backfill geocoding for existing jobs (one-time, then self-disabling)
        RecurringJob.AddOrUpdate<IGeocodingBackfillService>(
            "backfill-geocoding",
            service => service.BackfillAsync(100, CancellationToken.None),
            "*/10 * * * *"); // Every 10 minutes until all jobs are geocoded
    }
}

public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var connection = httpContext.Connection;
        var remoteIp = connection.RemoteIpAddress;

        // Allow only local connections
        if (remoteIp is not null)
        {
            return System.Net.IPAddress.IsLoopback(remoteIp)
                || remoteIp.Equals(connection.LocalIpAddress);
        }

        return false;
    }
}
