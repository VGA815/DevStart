using DevStart.SharedKernel;

namespace DevStart.Domain.Users
{
    public static class UserErrors
    {
        public static Error NotFound(Guid userId) => Error.NotFound(
            "Users.NotFound",
            $"The user with the Id = '{userId}' was not found");
        public static Error Unauthorized() => Error.Failure(
            "Users.Unauthorized",
            "You are not authorized to perform this action");
        public static readonly Error NotFoundByEmail = Error.NotFound(
            "Users.NotFoundByEmail",
            "The user with the specified email was not found");
        public static readonly Error EmailNotUnique = Error.Conflict(
            "Users.EmailNotUnique",
            "The provided email is not unique");
        public static readonly Error UsernameNotUnique = Error.Conflict(
            "Users.UsernameNotUnique",
            "The provided username is not nique");
        public static readonly Error NotFoundByUsername = Error.NotFound(
            "Users.NotFoundByUsername",
            "The user with the specified username was not found");
        public static readonly Error EmailNotVerified = Error.Forbidden(
            "Users.EmailNotVerified",
            "Email address is not verified. Please check your inbox and verify your email before logging in");
        public static readonly Error AlreadyVerified = Error.Conflict(
            "Users.AlreadyVerified",
            "The email address is already verified");
        public static readonly Error InvalidCurrentPassword = Error.Problem(
            "Users.InvalidCurrentPassword",
            "The current password is incorrect");
        public static readonly Error PasswordNotSet = Error.Conflict(
            "Users.PasswordNotSet",
            "This account has no password set (it uses an external login). Use the password reset flow to set one.");
        // Generic on purpose: the moderation reason is privacy-sensitive and is exposed only on
        // admin/audit surfaces (AdminActionLog, User.BanReason), never to the banned user.
        public static readonly Error Banned = Error.Forbidden(
            "Users.Banned",
            "This account has been banned.");
        public static readonly Error AlreadyBanned = Error.Conflict(
            "Users.AlreadyBanned",
            "The user is already banned");
        public static readonly Error NotBanned = Error.Conflict(
            "Users.NotBanned",
            "The user is not banned");
        public static readonly Error BanExpiryInPast = Error.Validation(
            "Users.BanExpiryInPast",
            "The ban expiry date must be in the future");
        public static readonly Error CannotBanSelf = Error.Validation(
            "Users.CannotBanSelf",
            "You cannot ban your own account");
        public static readonly Error CannotBanAdmin = Error.Forbidden(
            "Users.CannotBanAdmin",
            "Administrator accounts cannot be banned");
    }
}
