namespace DevStart.Application.Abstractions.Authentication
{
    public interface ITotpProvider
    {
        /// <summary>Generates a new random TOTP secret (20 bytes, Base32-encoded).</summary>
        string GenerateSecret();

        /// <summary>
        /// Verifies a 6-digit TOTP code against the secret within a ±1-step window. Rejects codes
        /// whose timestep is at or before <paramref name="lastUsedTimestep"/> (replay protection).
        /// On success, <paramref name="matchedTimestep"/> is the timestep the caller must persist.
        /// </summary>
        bool VerifyCode(string base32Secret, string code, long? lastUsedTimestep, out long matchedTimestep);

        /// <summary>Builds the otpauth:// URI encoded into the QR code by authenticator apps.</summary>
        string BuildOtpAuthUri(string base32Secret, string accountEmail);
    }
}
