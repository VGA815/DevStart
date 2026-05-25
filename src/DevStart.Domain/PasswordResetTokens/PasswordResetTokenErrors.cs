using DevStart.SharedKernel;

namespace DevStart.Domain.PasswordResetTokens
{
    public static class PasswordResetTokenErrors
    {
        public static Error NotFound(Guid tokenId) => Error.NotFound(
            "PasswordResetTokens.NotFound",
            $"The password reset token with tokenId = '{tokenId}' was not found or has expired");
    }
}
