using DevStart.Application.Abstractions.Data;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace DevStart.Infrastructure.Caching
{
    internal sealed class RedisCacheService : ICacheService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly IConnectionMultiplexer _multiplexer;
        private readonly IDatabase _db;
        private readonly RedisOptions _options;

        public RedisCacheService(
            IConnectionMultiplexer multiplexer,
            IOptions<RedisOptions> options)
        {
            _multiplexer = multiplexer;
            _db = multiplexer.GetDatabase();
            _options = options.Value;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            RedisValue value = await _db.StringGetAsync(BuildKey(key));

            if (!value.HasValue)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>((string)value!, SerializerOptions);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string json = JsonSerializer.Serialize(value, SerializerOptions);

            await _db.StringSetAsync(BuildKey(key), json, expiry: ttl);
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _db.KeyDeleteAsync(BuildKey(key));
        }

        public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string pattern = $"{BuildKey(prefix)}*";

            foreach (System.Net.EndPoint endpoint in _multiplexer.GetEndPoints())
            {
                IServer server = _multiplexer.GetServer(endpoint);


                await foreach (RedisKey key in server.KeysAsync(pattern: pattern, pageSize: 250).WithCancellation(cancellationToken))
                {
                    await _db.KeyDeleteAsync(key);
                }
            }
        }

        private string BuildKey(string key) => $"{_options.InstanceName}:{key}";
    }
}
