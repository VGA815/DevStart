using System.Collections.Concurrent;
using DevStart.Application.Abstractions.Data;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>
    /// In-memory <see cref="ICacheService"/> replacing Redis. Stores values by key so the caching
    /// decorator behaves realistically, but exposes <see cref="Clear"/> so the harness can flush it
    /// between tests — otherwise a value cached before a Respawn reset would survive into the next test
    /// while the underlying row is gone.
    /// </summary>
    internal sealed class InMemoryCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, object?> _values = new();

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (_values.TryGetValue(key, out object? value) && value is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult<T?>(default);
        }

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            foreach (string key in _values.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            {
                _values.TryRemove(key, out _);
            }

            return Task.CompletedTask;
        }

        public void Clear() => _values.Clear();
    }
}
