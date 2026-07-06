using DevStart.SharedKernel;

namespace DevStart.Domain.TwoFactor
{
    public static class TwoFactorErrors
    {
        public static readonly Error AlreadyEnabled = Error.Conflict(
            "TwoFactor.AlreadyEnabled",
            "Two-factor authentication is already enabled for this account");
        public static readonly Error NotEnabled = Error.Conflict(
            "TwoFactor.NotEnabled",
            "Two-factor authentication is not enabled for this account");
        public static readonly Error SetupNotStarted = Error.Conflict(
            "TwoFactor.SetupNotStarted",
            "Two-factor setup has not been started. Request a setup secret first");
        // Deliberately does not distinguish between a wrong TOTP code and a wrong recovery code.
        public static readonly Error InvalidCode = Error.Problem(
            "TwoFactor.InvalidCode",
            "The provided code is invalid or has already been used");
        public static readonly Error ChallengeExpired = Error.Problem(
            "TwoFactor.ChallengeExpired",
            "The two-factor challenge has expired or is invalid. Please log in again");
        public static readonly Error TooManyAttempts = Error.Problem(
            "TwoFactor.TooManyAttempts",
            "Too many incorrect codes. Please log in again");
        public static readonly Error SetupRequired = Error.Forbidden(
            "TwoFactor.SetupRequired",
            "Two-factor authentication setup is required for this account");
        // The admin route skips the password+code proof; admins disable their own 2FA via the
        // regular self-service endpoint like everyone else.
        public static readonly Error CannotResetSelf = Error.Validation(
            "TwoFactor.CannotResetSelf",
            "Use the self-service disable endpoint to remove two-factor authentication from your own account");
    }
}
