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
    }
}
