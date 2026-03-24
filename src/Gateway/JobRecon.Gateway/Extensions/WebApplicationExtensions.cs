using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace JobRecon.Gateway.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi(config =>
            {
                config.DocumentTitle = "JobRecon API Gateway";
                config.Path = "/swagger";
                config.DocumentPath = "/swagger/{documentName}/swagger.json";
            });
        }

        app.UseResponseCompression();
        app.UseIpRateLimiting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapEndpoints(this WebApplication app)
    {
        app.MapReverseProxy();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready");

        return app;
    }
}
