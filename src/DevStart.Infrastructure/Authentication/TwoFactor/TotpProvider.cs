using DevStart.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;
using OtpNet;

namespace DevStart.Infrastructure.Authentication.TwoFactor
{
    /// <summary>
    /// RFC 6238 TOTP: SHA-1, 6 digits, 30-second step — the parameter set every mainstream
    /// authenticator app supports. Verification accepts ±1 step of clock drift.
    /// </summary>
    internal sealed class TotpProvider(IOptions<TwoFactorOptions> options) : ITotpProvider
    {
        private const int SecretSizeBytes = 20;
        private const int StepSeconds = 30;
        private const int Digits = 6;

        private static readonly VerificationWindow Window = new(previous: 1, future: 1);

        private readonly string _issuer = options.Value.Issuer;

        public string GenerateSecret() =>
            Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(SecretSizeBytes));

        public bool VerifyCode(string base32Secret, string code, long? lastUsedTimestep, out long matchedTimestep)
        {
            matchedTimestep = 0;

            byte[] secretBytes;
            try
            {
                secretBytes = Base32Encoding.ToBytes(base32Secret);
            }
            catch (ArgumentException)
            {
                return false;
            }

            var totp = new Totp(secretBytes, step: StepSeconds, mode: OtpHashMode.Sha1, totpSize: Digits);
            if (!totp.VerifyTotp(code, out matchedTimestep, Window))
            {
                return false;
            }

            // Replay guard: never accept a timestep at or before the last accepted one.
            return lastUsedTimestep is null || matchedTimestep > lastUsedTimestep;
        }

        public string BuildOtpAuthUri(string base32Secret, string accountEmail)
        {
            string label = Uri.EscapeDataString($"{_issuer}:{accountEmail}");
            string issuer = Uri.EscapeDataString(_issuer);
            return $"otpauth://totp/{label}?secret={base32Secret}&issuer={issuer}&algorithm=SHA1&digits={Digits}&period={StepSeconds}";
        }
    }
}
