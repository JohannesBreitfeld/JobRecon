using Hangfire;
using Hangfire.PostgreSql;
using JobRecon.Jobs.Configuration;
using JobRecon.Jobs.Contracts;
using JobRecon.Jobs.Infrastructure;
using JobRecon.Jobs.Services;
using JobRecon.Jobs.Services.Fetchers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
        services.AddDbContext<JobsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("JobsDb"),
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "jobs")));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<HangfireSettings>(configuration.GetSection(HangfireSettings.SectionName));

        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IJobSourceService, JobSourceService>();
        services.AddScoped<IJobFetcherService, JobFetcherService>();
        services.AddScoped<IJobEnrichmentService, JobEnrichmentService>();

        // Register job fetchers
        services.AddHttpClient<JobTechLinksFetcher>()
            .AddPolicyHandler(GetRetryPolicy());
        services.AddScoped<IJobFetcher, JobTechLinksFetcher>();

        // HttpClient for enrichment service
        services.AddHttpClient<JobEnrichmentService>()
            .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    public static IServiceCollection AddJobsHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var hangfireSettings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>();
        var connectionString = hangfireSettings?.ConnectionString
            ?? configuration.GetConnectionString("JobsDb");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireSettings?.WorkerCount ?? 2;
            options.Queues = ["default", "jobs"];
        });

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
