using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace JobRecon.Profile.Grpc;

public sealed class ApiKeyInterceptor(
    IConfiguration configuration,
    ILogger<ApiKeyInterceptor> logger) : Interceptor
{
    private const string ApiKeyHeader = "x-api-key";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var expectedKey = configuration["GrpcApiKey"];
        if (string.IsNullOrEmpty(expectedKey))
        {
            logger.LogError("GrpcApiKey is not configured — rejecting all gRPC requests");
            throw new RpcException(new Status(StatusCode.Unauthenticated, "API key not configured on server."));
        }

        var apiKey = context.RequestHeaders.GetValue(ApiKeyHeader);
        var apiKeyBytes = Encoding.UTF8.GetBytes(apiKey ?? "");
        var expectedBytes = Encoding.UTF8.GetBytes(expectedKey);

        if (!CryptographicOperations.FixedTimeEquals(apiKeyBytes, expectedBytes))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid or missing API key."));
        }

        return await continuation(request, context);
    }
}
