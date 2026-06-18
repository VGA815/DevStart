using DevStart.Application.Abstractions.Authentication;
using StackExchange.Redis;
using System.Text.Json;

namespace DevStart.Infrastructure.Authentication.OAuth
{
    internal sealed class RedisPendingRegistrationStore : IPendingRegistrationStore
    {
        private const string KeyPrefix = "oauth:pending:";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly IDatabase _db;

        public RedisPendingRegistrationStore(IConnectionMultiplexer multiplexer)
        {
            _db = multiplexer.GetDatabase();
        }

        public Task SaveAsync(string token, PendingExternalRegistration entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string json = JsonSerializer.Serialize(entry, SerializerOptions);
            return _db.StringSetAsync(Key(token), json, expiry: ttl);
        }

        public async Task<PendingExternalRegistration?> ConsumeAsync(string token, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RedisValue value = await _db.StringGetDeleteAsync(Key(token));
            if (!value.HasValue)
            {
                return null;
            }
            return JsonSerializer.Deserialize<PendingExternalRegistration>((string)value!, SerializerOptions);
        }

        private static string Key(string token) => $"{KeyPrefix}{token}";
    }
}
