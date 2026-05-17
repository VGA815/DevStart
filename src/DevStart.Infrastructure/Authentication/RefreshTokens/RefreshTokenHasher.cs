using System.Security.Cryptography;
using System.Text;

namespace DevStart.Infrastructure.Authentication.RefreshTokens
{
    internal static class RefreshTokenHasher
    {
        public static string Hash(string rawToken)
        {
            byte[] bytes = SHA256.HashData(Encoding.ASCII.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}
