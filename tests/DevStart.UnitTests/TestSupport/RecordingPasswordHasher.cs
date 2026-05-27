using DevStart.Application.Abstractions.Authentication;
using DevStart.Infrastructure.Authentication;

namespace DevStart.UnitTests.TestSupport
{
    /// <summary>
    /// Wraps the real <see cref="PasswordHasher"/> while recording how many times <see cref="Verify"/>
    /// was called, so tests can assert the login handler runs the verifier even on the user-not-found
    /// path (the timing mitigation against account enumeration).
    /// </summary>
    internal sealed class RecordingPasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher _inner = new();

        public int VerifyCallCount { get; private set; }
        public List<string> VerifiedHashes { get; } = [];

        public string Hash(string password) => _inner.Hash(password);

        public bool Verify(string password, string passwordHash)
        {
            VerifyCallCount++;
            VerifiedHashes.Add(passwordHash);
            return _inner.Verify(password, passwordHash);
        }
    }
}
