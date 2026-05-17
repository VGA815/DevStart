namespace DevStart.Infrastructure.Authentication.RefreshTokens
{
    public sealed class RefreshTokenOptions
    {
        public int LifetimeDays { get; set; } = 30;
    }
}
