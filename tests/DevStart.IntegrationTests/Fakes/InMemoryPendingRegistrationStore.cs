using System.Collections.Concurrent;
using DevStart.Application.Abstractions.Authentication;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>In-memory <see cref="IPendingRegistrationStore"/> replacing the Redis-backed store used to
    /// hold an OAuth identity (or a re-consent challenge) awaiting consent acceptance. Single-use tokens.</summary>
    internal sealed class InMemoryPendingRegistrationStore : IPendingRegistrationStore
    {
        private readonly ConcurrentDictionary<string, PendingExternalRegistration> _entries = new();

        public Task SaveAsync(string token, PendingExternalRegistration entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _entries[token] = entry;
            return Task.CompletedTask;
        }

        public Task<PendingExternalRegistration?> ConsumeAsync(string token, CancellationToken cancellationToken)
        {
            _entries.TryRemove(token, out PendingExternalRegistration? entry);
            return Task.FromResult(entry);
        }
    }
}
