using System.Security.Claims;
using JobRecon.Identity.Contracts;
using JobRecon.Identity.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobRecon.Identity.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Authenticate user and get tokens")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshToken)
            .WithName("RefreshToken")
            .WithSummary("Refresh access token using refresh token")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Logout and revoke current refresh token")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout-all", LogoutAll)
            .WithName("LogoutAll")
            .WithSummary("Logout from all devices by revoking all refresh tokens")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithSummary("Get current authenticated user info")
            .RequireAuthorization()
            .Produces<UserInfo>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group.MapPost("/forgot-password", ForgotPassword)
            .WithName("ForgotPassword")
            .WithSummary("Request a password reset email")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("/reset-password", ResetPassword)
            .WithName("ResetPassword")
            .WithSummary("Reset password using a valid token")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("/confirm-email", ConfirmEmail)
            .WithName("ConfirmEmail")
            .WithSummary("Confirm email address using a valid token")
            .Produces<MessageResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("/resend-confirmation", ResendConfirmation)
            .WithName("ResendConfirmation")
            .WithSummary("Resend email confirmation")
            .RequireAuthorization()
            .Produces<MessageResponse>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "User.EmailExists" => Results.Conflict(result.Error),
                _ => Results.BadRequest(result.Error)
            };
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> RefreshToken(
        [FromBody] RefreshTokenRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RefreshTokenAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> Logout(
        [FromBody] RefreshTokenRequest? request,
        [FromServices] IAuthService authService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        await authService.LogoutAsync(userId.Value, request?.RefreshToken, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAll(
        [FromServices] IAuthService authService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        await authService.RevokeAllTokensAsync(userId.Value, cancellationToken);
        return Results.NoContent();
    }

    private static IResult GetCurrentUser(ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var firstName = user.FindFirstValue(ClaimTypes.GivenName);
        var lastName = user.FindFirstValue(ClaimTypes.Surname);
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        var userInfo = new UserInfo
        {
            Id = userId.Value,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true, // If they have a valid token, email was confirmed or not required
            Roles = roles
        };

        return Results.Ok(userInfo);
    }

    private static async Task<IResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        await authService.SendPasswordResetAsync(request.Email, cancellationToken);

        // Always return success to prevent email enumeration
        return Results.Ok(new MessageResponse("If an account with that email exists, a reset link has been sent."));
    }

    private static async Task<IResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(result.Error);
        }

        return Results.Ok(new MessageResponse("Password has been reset successfully."));
    }

    private static async Task<IResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.ConfirmEmailAsync(request, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(result.Error);
        }

        return Results.Ok(new MessageResponse("Email confirmed successfully."));
    }

    private static async Task<IResult> ResendConfirmation(
        [FromServices] IAuthService authService,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(user);
        if (userId is null)
        {
            return Results.Unauthorized();
        }

        var result = await authService.ResendEmailConfirmationAsync(userId.Value, cancellationToken);

        if (result.IsFailure)
        {
            return Results.BadRequest(result.Error);
        }

        return Results.Ok(new MessageResponse("Confirmation email has been sent."));
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
