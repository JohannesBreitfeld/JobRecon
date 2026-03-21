using System.Text;
using JobRecon.Matching.Configuration;
using JobRecon.Matching.Contracts;
using JobRecon.Matching.Services;
using JobRecon.Protos.Jobs;
using JobRecon.Protos.Profile;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace JobRecon.Matching.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMatchingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<GrpcServiceAddresses>(configuration.GetSection(GrpcServiceAddresses.SectionName));
        services.Configure<RabbitMqSettings>(configuration.GetSection(RabbitMqSettings.SectionName));

        var grpcAddresses = configuration.GetSection(GrpcServiceAddresses.SectionName).Get<GrpcServiceAddresses>()
            ?? new GrpcServiceAddresses();

        // Register matching service
        services.AddScoped<IMatchingService, MatchingService>();

        // Register event publisher
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // Register gRPC clients
        services.AddGrpcClient<ProfileGrpc.ProfileGrpcClient>(o =>
        {
            o.Address = new Uri(grpcAddresses.ProfileService);
        });

        services.AddGrpcClient<JobsGrpc.JobsGrpcClient>(o =>
        {
            o.Address = new Uri(grpcAddresses.JobsService);
        });

        services.AddScoped<IProfileClient, ProfileClient>();
        services.AddScoped<IJobsClient, JobsClient>();

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
}
