using Hangfire;
using Hangfire.Dashboard;
using JobRecon.Notifications.Infrastructure;
using JobRecon.Notifications.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

namespace JobRecon.Notifications.Extensions;

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
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public static void ConfigureRecurringJobs(this WebApplication app)
    {
        // Process digest emails every hour
        RecurringJob.AddOrUpdate<IDigestService>(
            "process-pending-digests",
            service => service.ProcessPendingDigestsAsync(CancellationToken.None),
            "0 * * * *"); // Every hour at minute 0

        // Cleanup old notifications daily at 3 AM
        RecurringJob.AddOrUpdate<IDigestService>(
            "cleanup-old-notifications",
            service => service.CleanupOldNotificationsAsync(30, CancellationToken.None),
            "0 3 * * *"); // Daily at 3:00 AM
    }
}

public sealed class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var connection = httpContext.Connection;
        var remoteIp = connection.RemoteIpAddress;

        // Allow only local connections (works in containers where hostname != localhost)
        if (remoteIp is not null)
        {
            return System.Net.IPAddress.IsLoopback(remoteIp)
                || remoteIp.Equals(connection.LocalIpAddress);
        }

        return false;
    }
}
