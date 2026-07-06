namespace DevStart.Application.Abstractions.Authentication
{
    /// <summary>
    /// Encrypts TOTP secrets for storage at rest. Unlike passwords, TOTP secrets must be
    /// recoverable (the server needs the plaintext to compute codes), so this is reversible
    /// encryption, not hashing.
    /// </summary>
    public interface ITwoFactorSecretProtector
    {
        string Protect(string plaintext);

        string Unprotect(string ciphertext);
    }
}
