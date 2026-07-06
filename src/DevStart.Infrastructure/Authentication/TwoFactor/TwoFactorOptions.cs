namespace DevStart.Infrastructure.Authentication.TwoFactor
{
    public sealed class TwoFactorOptions
    {
        public const string SectionName = "TwoFactor";

        /// <summary>Base64-encoded 32-byte AES-256 key used to encrypt TOTP secrets at rest.</summary>
        public string EncryptionKey { get; set; } = string.Empty;

        /// <summary>Issuer label shown in authenticator apps (otpauth:// URI).</summary>
        public string Issuer { get; set; } = "DevStart";

        public bool HasValidKey
        {
            get
            {
                if (string.IsNullOrWhiteSpace(EncryptionKey))
                {
                    return false;
                }
                try
                {
                    return Convert.FromBase64String(EncryptionKey).Length == 32;
                }
                catch (FormatException)
                {
                    return false;
                }
            }
        }
    }
}
