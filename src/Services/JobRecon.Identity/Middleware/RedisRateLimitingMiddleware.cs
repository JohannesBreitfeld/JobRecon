using System.Net;
using JobRecon.Identity.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace JobRecon.Identity.Middleware;

public sealed class RedisRateLimitingMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer redis,
    IOptions<RateLimitSettings> settings,
    ILogger<RedisRateLimitingMiddleware> logger)
{
    private readonly RateLimitSettings _settings = settings.Value;

    private static readonly LuaScript IncrementScript = LuaScript.Prepare(
        """
        local count = redis.call('INCR', @key)
        if count == 1 then
            redis.call('EXPIRE', @key, @ttl)
        end
        return count
        """);

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path is null || !context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var rule = _settings.Rules.FirstOrDefault(r =>
            path.Equals(r.Endpoint, StringComparison.OrdinalIgnoreCase));

        if (rule is null)
        {
            await next(context);
            return;
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"JobRecon:ratelimit:{ip}:{rule.Endpoint}";

        try
        {
            var db = redis.GetDatabase();
            var count = (long)await db.ScriptEvaluateAsync(IncrementScript, new
            {
                key = (RedisKey)key,
                ttl = rule.PeriodSeconds
            });

            context.Response.Headers["X-RateLimit-Limit"] = rule.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, rule.Limit - count).ToString();

            if (count > rule.Limit)
            {
                var ttl = await db.KeyTimeToLiveAsync(key);
                context.Response.Headers["Retry-After"] = ((int)(ttl?.TotalSeconds ?? rule.PeriodSeconds)).ToString();
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later."
                });
                return;
            }
        }
        catch (Exception ex)
        {
            // Graceful degradation: if Redis is unavailable, allow the request
            logger.LogWarning(ex, "Redis rate limiting unavailable, allowing request");
        }

        await next(context);
    }
}
