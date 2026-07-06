using DevStart.Domain.TwoFactor;

namespace DevStart.Application.Auth.TwoFactor
{
    /// <summary>
    /// Verifies a second-factor code — a 6-digit TOTP or a recovery code — against a user's
    /// enabled 2FA state and records its consumption (accepted timestep / used-up recovery code)
    /// on the tracked entities. The caller is responsible for SaveChanges, and must persist the
    /// consumption before acting on a successful result.
    /// </summary>
    public interface ITwoFactorCodeVerifier
    {
        Task<bool> VerifyAndConsumeAsync(UserTwoFactor twoFactor, string code, CancellationToken cancellationToken);
    }
}
