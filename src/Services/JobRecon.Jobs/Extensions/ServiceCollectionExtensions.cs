using Hangfire;
using Hangfire.PostgreSql;
using JobRecon.Domain.Common;
using JobRecon.Infrastructure.Persistence;
using JobRecon.Infrastructure.Messaging;
using JobRecon.Jobs.Configuration;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Infrastructure;
using JobRecon.Infrastructure.Caching;
using JobRecon.Jobs.Services;
using JobRecon.Jobs.Services.Fetchers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using System.Text;

namespace JobRecon.Jobs.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<DomainEventInterceptor>();

        services.AddDbContext<JobsDbContext>((sp, options) =>
        {
            options
                .UseNpgsql(
                    configuration.GetConnectionString("JobsDb"),
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "jobs"))
                .ConfigureWarnings(w => w.Ignore(
                    Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
        });

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<HangfireSettings>(configuration.GetSection(HangfireSettings.SectionName));
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));

        services.AddSingleton<IJobEventPublisher, RabbitMqJobEventPublisher>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<JobService>());
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddKeyedScoped<IJobService, JobService>("inner");
        services.AddScoped<IJobCacheInvalidator, JobCacheInvalidator>();
        services.AddScoped<IJobService, CachingJobService>();
        services.AddScoped<IJobSourceService, JobSourceService>();
        services.AddScoped<IJobFetcherService, JobFetcherService>();
        services.AddScoped<IJobEnrichmentService, JobEnrichmentService>();
        services.AddScoped<IJobExpirationService, JobExpirationService>();
        services.AddSingleton<IPlaywrightPageFactory, PlaywrightPageFactory>();
        services.AddScoped<ILocalityService, LocalityService>();
        services.AddScoped<ILocalityImportService, LocalityImportService>();
        services.AddScoped<IGeocodingService, GeocodingService>();
        services.AddScoped<IGeocodingBackfillService, GeocodingBackfillService>();

        services.AddRedisCache(configuration);

        // Register job fetchers
        services.AddHttpClient<JobTechLinksFetcher>()
            .AddPolicyHandler(GetRetryPolicy());
        services.AddScoped<IJobFetcher, JobTechLinksFetcher>();

        return services;
    }

    public static IServiceCollection AddJobsHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var hangfireSettings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>();
        var connectionString = string.IsNullOrEmpty(hangfireSettings?.ConnectionString)
            ? configuration.GetConnectionString("JobsDb")
            : hangfireSettings.ConnectionString;

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString),
                new PostgreSqlStorageOptions { SchemaName = "hangfire_jobs" }));

        if (hangfireSettings?.EnableServer ?? true)
        {
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = hangfireSettings?.WorkerCount ?? 2;
                options.Queues = ["jobs"];
            });
        }

        return services;
    }

    public static IServiceCollection AddJobsAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }
}
