using AspNetCoreRateLimit;
using StackExchange.Redis;

namespace JobRecon.Gateway.Middleware;

public static class ResilientRateLimitingExtensions
{
    private const string BranchCompletedKey = "__ratelimit_branch_completed";
    private static readonly TimeSpan CircuitOpenDuration = TimeSpan.FromSeconds(30);
    private static long _circuitOpenUntilTicks;

    public static IApplicationBuilder UseResilientIpRateLimiting(this IApplicationBuilder app)
    {
        var branchBuilder = app.New();
        branchBuilder.UseIpRateLimiting();
        branchBuilder.Run(context =>
        {
            context.Items[BranchCompletedKey] = true;
            return Task.CompletedTask;
        });
        var rateLimitBranch = branchBuilder.Build();

        return app.Use(async (context, next) =>
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("JobRecon.Gateway.ResilientRateLimiting");

            if (DateTime.UtcNow.Ticks < Interlocked.Read(ref _circuitOpenUntilTicks))
            {
                await next();
                return;
            }

            try
            {
                await rateLimitBranch(context);

                if (context.Items.ContainsKey(BranchCompletedKey))
                {
                    await next();
                }
            }
            catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
            {
                Interlocked.Exchange(
                    ref _circuitOpenUntilTicks,
                    DateTime.UtcNow.Add(CircuitOpenDuration).Ticks);

                logger.LogWarning(
                    ex,
                    "Redis unavailable; bypassing IP rate limiting for {Seconds}s",
                    CircuitOpenDuration.TotalSeconds);

                if (!context.Response.HasStarted)
                {
                    await next();
                }
            }
        });
    }
}
