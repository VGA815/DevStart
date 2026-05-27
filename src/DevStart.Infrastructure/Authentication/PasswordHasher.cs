using DevStart.Application.Abstractions.Authentication;
using System.Security.Cryptography;

namespace DevStart.Infrastructure.Authentication
{
    internal sealed class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 500000;

        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

        public string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

            return $"{Convert.ToHexString(hash)}-{Convert.ToHexString(salt)}";
        }

        public bool Verify(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(passwordHash))
            {
                return false;
            }

            string[] parts = passwordHash.Split('-');
            if (parts.Length != 2)
            {
                return false;
            }

            byte[] hash;
            byte[] salt;
            try
            {
                hash = Convert.FromHexString(parts[0]);
                salt = Convert.FromHexString(parts[1]);
            }
            catch (FormatException)
            {
                // A malformed stored hash must not surface as an unhandled 500 — treat it as a failed
                // verification (the account simply can't authenticate until the hash is reset).
                return false;
            }

            byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

            return CryptographicOperations.FixedTimeEquals(hash, inputHash);
        }
    }
}
