using DevStart.Application.Abstractions.Authentication;
using StackExchange.Redis;
using System.Text.Json;

namespace DevStart.Infrastructure.Authentication.OAuth
{
    internal sealed class RedisOAuthStateStore : IOAuthStateStore
    {
        private const string KeyPrefix = "oauth:state:";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly IDatabase _db;

        public RedisOAuthStateStore(IConnectionMultiplexer multiplexer)
        {
            _db = multiplexer.GetDatabase();
        }

        public Task SaveAsync(string state, OAuthStateEntry entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string json = JsonSerializer.Serialize(entry, SerializerOptions);
            return _db.StringSetAsync(Key(state), json, expiry: ttl);
        }

        public async Task<OAuthStateEntry?> ConsumeAsync(string state, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RedisValue value = await _db.StringGetDeleteAsync(Key(state));
            if (!value.HasValue)
            {
                return null;
            }
            return JsonSerializer.Deserialize<OAuthStateEntry>((string)value!, SerializerOptions);
        }

        private static string Key(string state) => $"{KeyPrefix}{state}";
    }
}
