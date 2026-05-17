using DevStart.SharedKernel;

namespace DevStart.Domain.RefreshTokens
{
    public static class RefreshTokenErrors
    {
        public static readonly Error Invalid = Error.Failure(
            "RefreshTokens.Invalid",
            "The refresh token is invalid");

        public static readonly Error Expired = Error.Failure(
            "RefreshTokens.Expired",
            "The refresh token has expired");

        public static readonly Error ReuseDetected = Error.Failure(
            "RefreshTokens.ReuseDetected",
            "Refresh token reuse detected; all sessions for this user have been revoked");
    }
}
