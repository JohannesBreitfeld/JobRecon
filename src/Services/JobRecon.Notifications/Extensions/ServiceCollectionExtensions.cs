using Hangfire;
using Hangfire.PostgreSql;
using JobRecon.Notifications.Configuration;
using JobRecon.Notifications.Contracts;
using JobRecon.Notifications.Infrastructure;
using JobRecon.Notifications.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using System.Text;

namespace JobRecon.Notifications.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("NotificationsDb"),
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "notifications")));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<HangfireSettings>(configuration.GetSection(HangfireSettings.SectionName));

        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IPreferenceService, PreferenceService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IDigestService, DigestService>();

        // Profile client for getting user emails
        services.AddHttpClient<IProfileClient, ProfileClient>(client =>
        {
            var baseUrl = configuration["ProfileService:BaseUrl"] ?? "http://localhost:5002";
            client.BaseAddress = new Uri(baseUrl);
        }).AddPolicyHandler(GetRetryPolicy());

        // RabbitMQ consumer as hosted service
        services.AddHostedService<JobMatchEventConsumer>();

        return services;
    }

    public static IServiceCollection AddNotificationsHangfire(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var hangfireSettings = configuration.GetSection(HangfireSettings.SectionName).Get<HangfireSettings>();
        var connectionString = hangfireSettings?.ConnectionString
            ?? configuration.GetConnectionString("NotificationsDb");

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
                options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireSettings?.WorkerCount ?? 2;
            options.Queues = ["default", "notifications"];
        });

        return services;
    }

    public static IServiceCollection AddNotificationsAuthentication(
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
