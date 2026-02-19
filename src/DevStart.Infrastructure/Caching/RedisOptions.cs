namespace DevStart.Infrastructure.Caching
{
    public sealed class RedisOptions
    {
        public string ConnectionString { get; init; } = default!;
        public string InstanceName { get; init; } = "app";
    }
}
