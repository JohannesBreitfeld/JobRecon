using Grpc.Core;
using JobRecon.Protos.Identity;

namespace JobRecon.Notifications.Contracts;

public interface IProfileClient
{
    Task<UserEmailDto?> GetUserEmailAsync(Guid userId, CancellationToken ct = default);
}

public record UserEmailDto(string Email, string? DisplayName);

public sealed class ProfileClient(
    IdentityGrpc.IdentityGrpcClient grpcClient,
    ILogger<ProfileClient> logger) : IProfileClient
{
    public async Task<UserEmailDto?> GetUserEmailAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await grpcClient.GetUserEmailAsync(
                new GetUserEmailRequest { UserId = userId.ToString() },
                cancellationToken: ct);

            return new UserEmailDto(
                response.Email,
                response.HasDisplayName ? response.DisplayName : null);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            logger.LogWarning("User not found for {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting email for user {UserId} via gRPC", userId);
            return null;
        }
    }
}
