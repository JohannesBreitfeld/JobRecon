using Hangfire;
using Hangfire.Dashboard;
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
        RecurringJob.AddOrUpdate<IJobFetcherService>(
            "fetch-all-jobs",
            service => service.FetchAllJobsAsync(CancellationToken.None),
            "0 */2 * * *"); // Every 2 hours
    }
}

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // In production, implement proper authorization
        var httpContext = context.GetHttpContext();
        return httpContext.Request.Host.Host == "localhost";
    }
}
