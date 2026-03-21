using Hangfire;
using Hangfire.Dashboard;
using JobRecon.Notifications.Infrastructure;
using JobRecon.Notifications.Services;
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

        app.MapHealthChecks("/health");

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

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.Request.Host.Host == "localhost";
    }
}
