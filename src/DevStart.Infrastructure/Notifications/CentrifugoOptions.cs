namespace DevStart.Infrastructure.Notifications
{
    public sealed class CentrifugoOptions
    {
        public string ApiUrl { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
        public string TokenHmacSecret { get; set; } = null!;
        public int TokenExpirationInMinutes { get; set; } = 10;
    }
}
