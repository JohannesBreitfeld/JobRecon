using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace JobRecon.Infrastructure.Caching;

public static class CachingExtensions
{
    public static IServiceCollection AddRedisCache(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisSettings = configuration
            .GetSection(RedisSettings.SectionName)
            .Get<RedisSettings>();

        var connectionString = redisSettings?.ConnectionString ?? string.Empty;

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = connectionString;
            options.InstanceName = "JobRecon:";
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(connectionString));

        services.AddHealthChecks()
            .AddRedis(connectionString, name: "redis");

        return services;
    }
}
