using System.Text;
using JobRecon.Matching.Clients;
using JobRecon.Matching.Configuration;
using JobRecon.Matching.Contracts;
using JobRecon.Matching.Services;
using JobRecon.Matching.Workers;
using JobRecon.Protos.Jobs;
using JobRecon.Protos.Profile;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Qdrant.Client;

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
        services.Configure<OllamaSettings>(configuration.GetSection(OllamaSettings.SectionName));
        services.Configure<QdrantSettings>(configuration.GetSection(QdrantSettings.SectionName));

        var grpcAddresses = configuration.GetSection(GrpcServiceAddresses.SectionName).Get<GrpcServiceAddresses>()
            ?? new GrpcServiceAddresses();

        // Register matching service
        services.AddScoped<IMatchingService, MatchingService>();

        // Register event publisher
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // Register gRPC clients with API key authentication
        var grpcApiKey = configuration["GrpcApiKey"] ?? "";

        services.AddGrpcClient<ProfileGrpc.ProfileGrpcClient>(o =>
        {
            o.Address = new Uri(grpcAddresses.ProfileService);
        }).AddCallCredentials((context, metadata) =>
        {
            if (!string.IsNullOrEmpty(grpcApiKey))
                metadata.Add("x-api-key", grpcApiKey);
            return Task.CompletedTask;
        }).ConfigureChannel(o => o.UnsafeUseInsecureChannelCallCredentials = true);

        services.AddGrpcClient<JobsGrpc.JobsGrpcClient>(o =>
        {
            o.Address = new Uri(grpcAddresses.JobsService);
        }).AddCallCredentials((context, metadata) =>
        {
            if (!string.IsNullOrEmpty(grpcApiKey))
                metadata.Add("x-api-key", grpcApiKey);
            return Task.CompletedTask;
        }).ConfigureChannel(o => o.UnsafeUseInsecureChannelCallCredentials = true);

        services.AddScoped<IProfileClient, ProfileClient>();
        services.AddScoped<IJobsClient, JobsClient>();

        // Ollama client for embeddings
        var ollamaSettings = configuration.GetSection(OllamaSettings.SectionName).Get<OllamaSettings>()
            ?? new OllamaSettings();
        services.AddHttpClient<IOllamaClient, OllamaClient>(client =>
        {
            client.BaseAddress = new Uri(ollamaSettings.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Qdrant vector store
        var qdrantSettings = configuration.GetSection(QdrantSettings.SectionName).Get<QdrantSettings>()
            ?? new QdrantSettings();
        services.AddSingleton(_ => new QdrantClient(qdrantSettings.Host, qdrantSettings.GrpcPort));
        services.AddSingleton<IVectorStore, QdrantVectorStore>();

        // Background worker for embedding jobs
        services.AddHostedService<JobEmbeddingWorker>();

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
