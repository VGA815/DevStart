using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace DevStart.Infrastructure.Authentication.TrustedDevices
{
    /// <summary>
    /// Deliberately not <c>RefreshTokenHasher</c>: that one hashes with <see cref="Encoding.ASCII"/>,
    /// which maps every non-ASCII byte to '?'. A refresh token only ever comes back from our own
    /// generator, but a device token arrives from localStorage and can be arbitrary — UTF-8 plus a
    /// shape check keeps distinct inputs distinct and stops garbage before it reaches the database.
    /// </summary>
    internal static class TrustedDeviceTokenHasher
    {
        /// <summary>32 random bytes rendered as unpadded URL-safe base64.</summary>
        internal const int TokenLength = 43;

        private static readonly SearchValues<char> Base64UrlChars = SearchValues.Create(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

        public static bool IsWellFormed(string? rawToken)
        {
            if (rawToken is null || rawToken.Length != TokenLength)
            {
                return false;
            }

            return !rawToken.AsSpan().ContainsAnyExcept(Base64UrlChars);
        }

        public static string Hash(string rawToken)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }

        public static string Generate()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
