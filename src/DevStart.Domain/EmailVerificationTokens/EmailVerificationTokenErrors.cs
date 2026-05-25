using DevStart.SharedKernel;

namespace DevStart.Domain.EmailVerificationTokens
{
    public static class EmailVerificationTokenErrors
    {
        public static Error NotFound(Guid tokenId) => Error.NotFound(
            "EmailVerificationTokens.NotFound",
            $"The email verification token with tokenId = '{tokenId}' was not found");
        public static readonly Error NotFoundByTokenId = Error.NotFound(
            "EmailVerificationTokens.NotFoundByTokenId",
            "The email email verification token with specified tokenId was not found");
        public static readonly Error NotFoundByUserId = Error.NotFound(
            "EmailVerificationTokens.NotFoundByUserId",
            "The email email verification token with specified userId was not found");
        public static readonly Error ResendTooSoon = Error.Conflict(
            "EmailVerificationTokens.ResendTooSoon",
            "A verification email was sent recently. Please wait a minute before requesting another.");
    }
}
