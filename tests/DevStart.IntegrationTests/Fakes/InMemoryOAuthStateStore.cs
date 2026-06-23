using System.Collections.Concurrent;
using DevStart.Application.Abstractions.Authentication;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>In-memory <see cref="IOAuthStateStore"/> replacing the Redis-backed store. TTLs are ignored
    /// (tests complete well within them); a state entry is single-use, consumed on first read.</summary>
    internal sealed class InMemoryOAuthStateStore : IOAuthStateStore
    {
        private readonly ConcurrentDictionary<string, OAuthStateEntry> _entries = new();

        public Task SaveAsync(string state, OAuthStateEntry entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _entries[state] = entry;
            return Task.CompletedTask;
        }

        public Task<OAuthStateEntry?> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            _entries.TryRemove(state, out OAuthStateEntry? entry);
            return Task.FromResult(entry);
        }
    }
}
