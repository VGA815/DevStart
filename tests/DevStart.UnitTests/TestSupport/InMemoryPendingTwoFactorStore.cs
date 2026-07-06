using DevStart.Application.Abstractions.Authentication;

namespace DevStart.UnitTests.TestSupport
{
    public sealed class InMemoryPendingTwoFactorStore : IPendingTwoFactorStore
    {
        public Dictionary<string, PendingTwoFactorLogin> Items { get; } = new();
        public Dictionary<string, long> Attempts { get; } = new();

        public Task SaveAsync(string token, PendingTwoFactorLogin entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            Items[token] = entry;
            return Task.CompletedTask;
        }

        public Task<PendingTwoFactorLogin?> GetAsync(string token, CancellationToken cancellationToken)
        {
            Items.TryGetValue(token, out PendingTwoFactorLogin? entry);
            return Task.FromResult(entry);
        }

        public Task RemoveAsync(string token, CancellationToken cancellationToken)
        {
            Items.Remove(token);
            Attempts.Remove(token);
            return Task.CompletedTask;
        }

        public Task<long> IncrementAttemptsAsync(string token, CancellationToken cancellationToken)
        {
            Attempts[token] = Attempts.GetValueOrDefault(token) + 1;
            return Task.FromResult(Attempts[token]);
        }
    }
}
