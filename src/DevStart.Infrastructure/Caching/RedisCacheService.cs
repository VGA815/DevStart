using DevStart.Application.Abstractions.Data;
using DevStart.SharedKernel;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace DevStart.Infrastructure.Caching
{
    internal sealed class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly RedisOptions _options;
        private readonly JsonSerializerOptions _serializerOptions;
        public RedisCacheService(
            IConnectionMultiplexer multiplexer,
            IOptions<RedisOptions> options)
        {
            _db = multiplexer.GetDatabase();
            _options = options.Value;
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }
        private string BuildKey(string key)
            => $"{_options.InstanceName}:{key}";
        public async Task<Result<T>?> GetAsync<T>(string key)
        {
            var value = await _db.StringGetAsync(BuildKey(key));

            if (!value.HasValue) return default;

            return JsonSerializer.Deserialize<Result<T>?>(value!.ToString(), _serializerOptions);
        }

        public async Task RemoveAsync(string key)
        {
            await _db.KeyDeleteAsync(BuildKey(key));
        }

        public Task RemoveByPrefixAsync(string prefix)
        {
            throw new NotImplementedException();
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
        {
            var json = JsonSerializer.Serialize(value, _serializerOptions);

            await _db.StringSetAsync(BuildKey(key), json, new Expiration(ttl));
        }
    }
}
