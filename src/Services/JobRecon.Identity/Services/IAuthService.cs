using JobRecon.Domain.Common;
using JobRecon.Identity.Contracts;

namespace JobRecon.Identity.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<Result> LogoutAsync(Guid userId, string? refreshToken = null, CancellationToken cancellationToken = default);
    Task<Result> RevokeAllTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> SendPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResendEmailConfirmationAsync(Guid userId, CancellationToken cancellationToken = default);
}
