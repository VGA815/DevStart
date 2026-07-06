using DevStart.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace DevStart.Infrastructure.Authentication.TwoFactor
{
    /// <summary>
    /// AES-256-GCM with a random 96-bit nonce per encryption. Storage format is
    /// <c>v1:base64(nonce || tag || ciphertext)</c>; the version prefix leaves room for future
    /// key rotation without a data migration.
    /// </summary>
    internal sealed class AesGcmTwoFactorSecretProtector : ITwoFactorSecretProtector
    {
        private const string VersionPrefix = "v1:";
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly byte[] _key;

        public AesGcmTwoFactorSecretProtector(IOptions<TwoFactorOptions> options)
        {
            _key = Convert.FromBase64String(options.Value.EncryptionKey);
        }

        public string Protect(string plaintext)
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            byte[] payload = new byte[NonceSize + TagSize + cipherBytes.Length];
            nonce.CopyTo(payload, 0);
            tag.CopyTo(payload, NonceSize);
            cipherBytes.CopyTo(payload, NonceSize + TagSize);

            return VersionPrefix + Convert.ToBase64String(payload);
        }

        public string Unprotect(string ciphertext)
        {
            if (!ciphertext.StartsWith(VersionPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Unknown two-factor secret format version.");
            }

            byte[] payload;
            try
            {
                payload = Convert.FromBase64String(ciphertext[VersionPrefix.Length..]);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Two-factor secret is not valid base64.", ex);
            }

            if (payload.Length < NonceSize + TagSize)
            {
                throw new InvalidOperationException("Two-factor secret ciphertext is truncated.");
            }

            byte[] nonce = payload[..NonceSize];
            byte[] tag = payload[NonceSize..(NonceSize + TagSize)];
            byte[] cipherBytes = payload[(NonceSize + TagSize)..];
            byte[] plainBytes = new byte[cipherBytes.Length];

            try
            {
                using var aes = new AesGcm(_key, TagSize);
                aes.Decrypt(nonce, cipherBytes, tag, plainBytes);
            }
            catch (CryptographicException ex)
            {
                // Authentication-tag mismatch: corrupt data or a changed/wrong encryption key.
                throw new InvalidOperationException("Two-factor secret could not be decrypted.", ex);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
