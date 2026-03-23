using Hangfire;
using Hangfire.Dashboard;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Infrastructure;
using JobRecon.Jobs.Services;
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

        app.MapHealthChecks("/health");

        return app;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<JobsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static void ConfigureRecurringJobs(this WebApplication app)
    {
        // Fetch jobs daily at 6 AM (daily files are published once per day)
        RecurringJob.AddOrUpdate<IJobFetcherService>(
            "fetch-all-jobs",
            service => service.FetchAllJobsAsync(CancellationToken.None),
            "0 6 * * *"); // Daily at 6:00 AM

        // Enrich pending jobs every 15 minutes
        RecurringJob.AddOrUpdate<IJobEnrichmentService>(
            "enrich-pending-jobs",
            service => service.EnrichPendingJobsAsync(50, CancellationToken.None),
            "*/15 * * * *"); // Every 15 minutes
    }
}

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
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
