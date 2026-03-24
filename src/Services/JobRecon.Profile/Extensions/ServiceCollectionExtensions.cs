using JobRecon.Domain.Common;
using JobRecon.Infrastructure.Persistence;
using JobRecon.Profile.Configuration;
using JobRecon.Profile.Infrastructure;
using JobRecon.Profile.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using System.Text;

namespace JobRecon.Profile.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProfileServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<DomainEventInterceptor>();

        services.AddDbContext<ProfileDbContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("ProfileDb"),
                npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "profile"));
            options.AddInterceptors(sp.GetRequiredService<DomainEventInterceptor>());
        });

        services.Configure<MinioSettings>(configuration.GetSection(MinioSettings.SectionName));
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var minioSettings = configuration.GetSection(MinioSettings.SectionName).Get<MinioSettings>()!;

        services.AddMinio(configureClient => configureClient
            .WithEndpoint(minioSettings.Endpoint)
            .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey)
            .WithSSL(minioSettings.UseSSL)
            .Build());

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<ProfileService>());
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        services.AddScoped<IFileStorageService, MinioFileStorageService>();
        services.AddScoped<IProfileService, ProfileService>();

        return services;
    }

    public static IServiceCollection AddProfileAuthentication(
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
