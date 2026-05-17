using DevStart.Application.Abstractions.Authentication;
using System.Collections.Concurrent;

namespace DevStart.UnitTests.TestSupport
{
    internal sealed class InMemoryOAuthStateStore : IOAuthStateStore
    {
        private readonly ConcurrentDictionary<string, OAuthStateEntry> _store = new();

        public Task SaveAsync(string state, OAuthStateEntry entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _store[state] = entry;
            return Task.CompletedTask;
        }

        public Task<OAuthStateEntry?> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            _store.TryRemove(state, out OAuthStateEntry? entry);
            return Task.FromResult(entry);
        }
    }
}
