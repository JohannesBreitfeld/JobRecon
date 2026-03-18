using JobRecon.Domain.Common;

namespace JobRecon.Domain.Identity;

public static class IdentityErrors
{
    public static class User
    {
        public static readonly Error NotFound = Error.NotFound(
            "User.NotFound",
            "User was not found.");

        public static readonly Error EmailAlreadyExists = Error.Conflict(
            "User.EmailAlreadyExists",
            "A user with this email already exists.");

        public static readonly Error InvalidCredentials = Error.Unauthorized(
            "User.InvalidCredentials",
            "Invalid email or password.");

        public static readonly Error EmailNotConfirmed = Error.Unauthorized(
            "User.EmailNotConfirmed",
            "Email address has not been confirmed.");

        public static readonly Error AccountDeactivated = Error.Unauthorized(
            "User.AccountDeactivated",
            "This account has been deactivated.");

        public static readonly Error InvalidEmail = Error.Validation(
            "User.InvalidEmail",
            "The email address is invalid.");

        public static readonly Error WeakPassword = Error.Validation(
            "User.WeakPassword",
            "Password does not meet security requirements.");
    }

    public static class Token
    {
        public static readonly Error InvalidToken = Error.Unauthorized(
            "Token.Invalid",
            "The token is invalid.");

        public static readonly Error ExpiredToken = Error.Unauthorized(
            "Token.Expired",
            "The token has expired.");

        public static readonly Error RevokedToken = Error.Unauthorized(
            "Token.Revoked",
            "The token has been revoked.");

        public static readonly Error InvalidRefreshToken = Error.Unauthorized(
            "Token.InvalidRefreshToken",
            "The refresh token is invalid or expired.");
    }

    public static class ExternalLogin
    {
        public static readonly Error ProviderNotSupported = Error.Validation(
            "ExternalLogin.ProviderNotSupported",
            "The external login provider is not supported.");

        public static readonly Error AlreadyLinked = Error.Conflict(
            "ExternalLogin.AlreadyLinked",
            "This external account is already linked to another user.");

        public static readonly Error NotLinked = Error.NotFound(
            "ExternalLogin.NotLinked",
            "No external login found for this provider.");
    }
}
