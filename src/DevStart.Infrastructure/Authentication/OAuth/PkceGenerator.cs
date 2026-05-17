using DevStart.Application.Abstractions.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace DevStart.Infrastructure.Authentication.OAuth
{
    internal sealed class PkceGenerator : IPkceGenerator
    {
        public PkcePair Create()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            string verifier = Base64UrlEncode(bytes);

            byte[] challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
            string challenge = Base64UrlEncode(challengeBytes);

            return new PkcePair(verifier, challenge);
        }

        private static string Base64UrlEncode(byte[] bytes) =>
            Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
    }
}
