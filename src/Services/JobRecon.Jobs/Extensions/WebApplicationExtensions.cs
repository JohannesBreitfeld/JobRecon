using Hangfire;
using Hangfire.Dashboard;
using JobRecon.Jobs.Configuration;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Domain;
using JobRecon.Jobs.Infrastructure;
using JobRecon.Jobs.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

        // Seed default job source if none exist
        if (!await dbContext.JobSources.AnyAsync())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobsDbContext>>();
            logger.LogInformation("Seeding default JobTech Links job source");

            dbContext.JobSources.Add(new JobSource
            {
                Id = Guid.NewGuid(),
                Name = "JobTech Links (Arbetsförmedlingen)",
                Type = JobSourceType.JobTechLinks,
                BaseUrl = "https://data.jobtechdev.se/annonser/jobtechlinks",
                IsEnabled = true,
                FetchIntervalMinutes = 60
            });

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Default job source seeded successfully");
        }
    }

    public static void ConfigureRecurringJobs(this WebApplication app)
    {
        var hangfireSettings = app.Services.GetRequiredService<IOptions<HangfireSettings>>().Value;
        if (!hangfireSettings.EnableServer)
        {
            return;
        }

        // Fetch jobs hourly — each run processes one day file and checkpoints,
        // so the next run picks up the next day. Once caught up, it's a fast no-op.
        RecurringJob.AddOrUpdate<IJobFetcherService>(
            "fetch-all-jobs",
            "jobs",
            service => service.FetchAllJobsAsync(CancellationToken.None),
            "0 * * * *");

        // Enrich pending jobs every 5 minutes — 100 jobs per batch with 5 concurrent
        // Playwright pages. Rate limiter throttles per-domain to avoid hammering any
        // single site. ~30 jobs/min throughput, catches up 60k backlog in ~33 hours.
        RecurringJob.AddOrUpdate<IJobEnrichmentService>(
            "enrich-pending-jobs",
            "jobs",
            service => service.EnrichPendingJobsAsync(100, CancellationToken.None),
            "*/5 * * * *");

        // Expire jobs with passed application deadlines — runs once daily at 00:01
        // since deadlines are date-granularity, no need to run more often
        RecurringJob.AddOrUpdate<IJobExpirationService>(
            "expire-jobs",
            "jobs",
            service => service.ExpireJobsAsync(CancellationToken.None),
            "1 0 * * *");

        // Backfill geocoding for existing jobs (one-time, then self-disabling)
        RecurringJob.AddOrUpdate<IGeocodingBackfillService>(
            "backfill-geocoding",
            "jobs",
            service => service.BackfillAsync(5000, CancellationToken.None),
            "*/10 * * * *");
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
