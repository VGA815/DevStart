using DevStart.Domain.TwoFactor;
using DevStart.Infrastructure.Authentication.TwoFactor;
using Microsoft.Extensions.Options;
using OtpNet;

namespace DevStart.UnitTests.TestSupport
{
    /// <summary>
    /// Real crypto components (TOTP, AES-GCM protector, recovery codes) wired with a fixed test
    /// key, plus helpers to compute valid/invalid codes for a secret. TOTP codes are computed
    /// against the real system clock — Otp.NET does not use IDateTimeProvider.
    /// </summary>
    internal static class TwoFactorTestKit
    {
        public static readonly IOptions<TwoFactorOptions> TestOptions = Options.Create(new TwoFactorOptions
        {
            EncryptionKey = Convert.ToBase64String(Enumerable.Range(0, 32).Select(i => (byte)i).ToArray()),
            Issuer = "DevStart-Tests",
        });

        public static TotpProvider CreateTotpProvider() => new(TestOptions);

        public static AesGcmTwoFactorSecretProtector CreateProtector() => new(TestOptions);

        public static RecoveryCodeGenerator CreateRecoveryCodeGenerator() => new();

        /// <summary>Seeds an enabled 2FA row; LastUsedTimestep=0 lies far in the past, so any current code passes.</summary>
        public static (UserTwoFactor TwoFactor, string Secret) CreateEnabled(Guid userId, DateTime now)
        {
            TotpProvider totp = CreateTotpProvider();
            string secret = totp.GenerateSecret();
            UserTwoFactor twoFactor = UserTwoFactor.CreatePending(userId, CreateProtector().Protect(secret), now);
            twoFactor.Enable(confirmedTimestep: 0, now);
            twoFactor.ClearDomainEvents();
            return (twoFactor, secret);
        }

        public static string CurrentCodeFor(string base32Secret, int stepOffset = 0)
        {
            var totp = new Totp(Base32Encoding.ToBytes(base32Secret), step: 30, mode: OtpHashMode.Sha1, totpSize: 6);
            return totp.ComputeTotp(DateTime.UtcNow.AddSeconds(stepOffset * 30));
        }

        /// <summary>A 6-digit code guaranteed not to match the secret within the ±1-step window.</summary>
        public static string WrongCodeFor(string base32Secret)
        {
            var valid = new HashSet<string>
            {
                CurrentCodeFor(base32Secret, -1),
                CurrentCodeFor(base32Secret, 0),
                CurrentCodeFor(base32Secret, 1),
            };
            for (int i = 0; ; i++)
            {
                string candidate = i.ToString("D6");
                if (!valid.Contains(candidate))
                {
                    return candidate;
                }
            }
        }
    }
}
