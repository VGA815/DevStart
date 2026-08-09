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

        /// <summary>
        /// Also returned when the session belongs to somebody else: a distinct "forbidden" would let a
        /// caller probe which session ids exist.
        /// </summary>
        public static readonly Error SessionNotFound = Error.NotFound(
            "Sessions.NotFound",
            "The session was not found");
    }
}
