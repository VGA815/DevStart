using System.Collections.Concurrent;
using DevStart.Application.Abstractions.Authentication;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>In-memory <see cref="IPendingTwoFactorStore"/> replacing the Redis-backed store used to
    /// hold logins that passed the first factor and are awaiting a TOTP code or mandatory enrollment.</summary>
    internal sealed class InMemoryPendingTwoFactorStore : IPendingTwoFactorStore
    {
        private readonly ConcurrentDictionary<string, PendingTwoFactorLogin> _entries = new();
        private readonly ConcurrentDictionary<string, long> _attempts = new();

        public Task SaveAsync(string token, PendingTwoFactorLogin entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _entries[token] = entry;
            return Task.CompletedTask;
        }

        public Task<PendingTwoFactorLogin?> GetAsync(string token, CancellationToken cancellationToken)
        {
            _entries.TryGetValue(token, out PendingTwoFactorLogin? entry);
            return Task.FromResult(entry);
        }

        public Task RemoveAsync(string token, CancellationToken cancellationToken)
        {
            _entries.TryRemove(token, out _);
            _attempts.TryRemove(token, out _);
            return Task.CompletedTask;
        }

        public Task<long> IncrementAttemptsAsync(string token, CancellationToken cancellationToken)
        {
            return Task.FromResult(_attempts.AddOrUpdate(token, 1, (_, current) => current + 1));
        }
    }
}
