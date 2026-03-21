using Grpc.Core;
using JobRecon.Identity.Domain;
using JobRecon.Protos.Identity;
using Microsoft.AspNetCore.Identity;

namespace JobRecon.Identity.Grpc;

public sealed class IdentityGrpcService(
    UserManager<ApplicationUser> userManager,
    ILogger<IdentityGrpcService> logger) : IdentityGrpc.IdentityGrpcBase
{
    public override async Task<UserEmailResponse> GetUserEmail(
        GetUserEmailRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid user ID format."));
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User {userId} not found."));
        }

        logger.LogDebug("Returning email for user {UserId} via gRPC", userId);

        return new UserEmailResponse
        {
            Email = user.Email ?? string.Empty,
            DisplayName = user.FullName
        };
    }
}
