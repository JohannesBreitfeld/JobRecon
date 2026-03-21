using System.Text;
using JobRecon.Matching.Configuration;
using JobRecon.Matching.Contracts;
using JobRecon.Matching.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;

namespace JobRecon.Matching.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMatchingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<ServiceUrls>(configuration.GetSection(ServiceUrls.SectionName));

        var serviceUrls = configuration.GetSection(ServiceUrls.SectionName).Get<ServiceUrls>()
            ?? new ServiceUrls();

        // Register matching service
        services.AddScoped<IMatchingService, MatchingService>();

        // Register HTTP clients for external services
        services.AddHttpClient<IProfileClient, ProfileClient>(client =>
            {
                client.BaseAddress = new Uri(serviceUrls.ProfileService);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy());

        services.AddHttpClient<IJobsClient, JobsClient>(client =>
            {
                client.BaseAddress = new Uri(serviceUrls.JobsService);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddPolicyHandler(GetRetryPolicy());

        // Add memory cache for caching profile/job data
        services.AddMemoryCache();

        return services;
    }

    public static IServiceCollection AddMatchingAuthentication(
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
