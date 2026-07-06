using DevStart.Application.Abstractions.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace DevStart.Infrastructure.Authentication.TwoFactor
{
    /// <summary>
    /// Recovery codes: 10 characters from the Crockford Base32 alphabet (~50 bits of entropy),
    /// displayed as XXXX-XXXX-XX. Stored as SHA-256 hex of the normalized code — a fast hash is
    /// safe because the codes are random, not user-chosen (same rationale as refresh tokens).
    /// </summary>
    internal sealed class RecoveryCodeGenerator : IRecoveryCodeGenerator
    {
        // Crockford Base32: no I, L, O, U — avoids transcription mistakes.
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        private const int CodeLength = 10;

        public IReadOnlyList<string> Generate(int count)
        {
            // Distinct on purpose: a duplicate would collide on the unique (user_id, code_hash)
            // index and fail SaveChanges. Collisions are astronomically unlikely at ~50 bits, so
            // this loop effectively runs exactly `count` times.
            var codes = new HashSet<string>(count);
            while (codes.Count < count)
            {
                char[] chars = RandomNumberGenerator.GetItems<char>(Alphabet, CodeLength);
                codes.Add($"{new string(chars, 0, 4)}-{new string(chars, 4, 4)}-{new string(chars, 8, 2)}");
            }
            return [.. codes];
        }

        public string Hash(string code)
        {
            string normalized = Normalize(code);
            byte[] bytes = SHA256.HashData(Encoding.ASCII.GetBytes(normalized));
            return Convert.ToHexString(bytes);
        }

        private static string Normalize(string code) =>
            code.Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
    }
}
