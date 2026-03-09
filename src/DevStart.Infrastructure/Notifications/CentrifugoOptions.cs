namespace DevStart.Infrastructure.Notifications
{
    public sealed class CentrifugoOptions
    {
        public string ApiUrl { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
    }
}
