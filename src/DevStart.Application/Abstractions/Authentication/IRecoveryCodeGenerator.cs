namespace DevStart.Application.Abstractions.Authentication
{
    public interface IRecoveryCodeGenerator
    {
        /// <summary>Generates high-entropy single-use recovery codes (shown to the user once).</summary>
        IReadOnlyList<string> Generate(int count);

        /// <summary>
        /// Normalizes (uppercase, dashes stripped) and hashes a code for storage/lookup. A fast
        /// SHA-256 hash is sufficient because codes are random, not user-chosen.
        /// </summary>
        string Hash(string code);
    }
}
