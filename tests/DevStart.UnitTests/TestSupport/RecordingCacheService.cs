using DevStart.Application.Abstractions.Data;
using System.Collections.Concurrent;

namespace DevStart.UnitTests.TestSupport
{
    /// <summary>In-memory <see cref="ICacheService"/> that also records the TTL passed to the last
    /// <c>SetAsync</c> per key, so tests can assert TTL clamping behaviour.</summary>
    internal sealed class RecordingCacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, object?> _values = new();

        public Dictionary<string, TimeSpan> LastTtl { get; } = new();

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
            LastTtl[key] = ttl;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            foreach (string key in _values.Keys.Where(k => k.StartsWith(prefix)).ToList())
            {
                _values.TryRemove(key, out _);
            }
            return Task.CompletedTask;
        }
    }
}
