using DevStart.Domain.Users;
using DevStart.SharedKernel;

namespace DevStart.Application.Auth.TwoFactor
{
    public sealed record TwoFactorSetupData(string Secret, string OtpAuthUri);

    /// <summary>
    /// TOTP enrollment shared by the authenticated self-service flow (api/users/me/2fa/*) and the
    /// login-time mandatory-setup flow for admins (api/auth/2fa/setup*).
    /// </summary>
    public interface ITwoFactorEnrollmentService
    {
        /// <summary>
        /// Creates (or rotates, while unconfirmed) the pending TOTP secret and persists it.
        /// Fails with <c>TwoFactor.AlreadyEnabled</c> when 2FA is already active.
        /// </summary>
        Task<Result<TwoFactorSetupData>> StartAsync(User user, CancellationToken cancellationToken);

        /// <summary>
        /// Confirms the pending secret with a first TOTP code, activates 2FA and persists a fresh
        /// set of recovery codes. Returns the plaintext codes — the only time they are visible.
        /// </summary>
        Task<Result<IReadOnlyList<string>>> ConfirmAsync(Guid userId, string code, CancellationToken cancellationToken);
    }
}
