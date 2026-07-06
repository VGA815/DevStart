using DevStart.Application.Abstractions.Authentication;
using StackExchange.Redis;
using System.Text.Json;

namespace DevStart.Infrastructure.Authentication.TwoFactor
{
    internal sealed class RedisPendingTwoFactorStore : IPendingTwoFactorStore
    {
        private const string KeyPrefix = "2fa:pending:";
        private const string AttemptsKeyPrefix = "2fa:attempts:";

        /// <summary>Outlives any challenge TTL so the counter cannot reset mid-challenge.</summary>
        private static readonly TimeSpan AttemptsTtl = TimeSpan.FromMinutes(15);

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly IDatabase _db;

        public RedisPendingTwoFactorStore(IConnectionMultiplexer multiplexer)
        {
            _db = multiplexer.GetDatabase();
        }

        public Task SaveAsync(string token, PendingTwoFactorLogin entry, TimeSpan ttl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string json = JsonSerializer.Serialize(entry, SerializerOptions);
            return _db.StringSetAsync(Key(token), json, expiry: ttl);
        }

        public async Task<PendingTwoFactorLogin?> GetAsync(string token, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RedisValue value = await _db.StringGetAsync(Key(token));
            if (!value.HasValue)
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<PendingTwoFactorLogin>((string)value!, SerializerOptions);
            }
            catch (JsonException)
            {
                // Corrupt payload: drop it and behave like an expired challenge (forces a re-login)
                // rather than 500-ing the verify/setup endpoint.
                await _db.KeyDeleteAsync([Key(token), AttemptsKey(token)]);
                return null;
            }
        }

        public Task RemoveAsync(string token, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return _db.KeyDeleteAsync([Key(token), AttemptsKey(token)]);
        }

        public async Task<long> IncrementAttemptsAsync(string token, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long attempts = await _db.StringIncrementAsync(AttemptsKey(token));
            await _db.KeyExpireAsync(AttemptsKey(token), AttemptsTtl);
            return attempts;
        }

        private static string Key(string token) => $"{KeyPrefix}{token}";

        private static string AttemptsKey(string token) => $"{AttemptsKeyPrefix}{token}";
    }
}
