using Grpc.Core;
using Grpc.Core.Interceptors;

namespace JobRecon.Jobs.Grpc;

public sealed class ApiKeyInterceptor(IConfiguration configuration) : Interceptor
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
            // If no key is configured, allow all requests (dev mode)
            return await continuation(request, context);
        }

        var apiKey = context.RequestHeaders.GetValue(ApiKeyHeader);
        if (apiKey != expectedKey)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid or missing API key."));
        }

        return await continuation(request, context);
    }
}
