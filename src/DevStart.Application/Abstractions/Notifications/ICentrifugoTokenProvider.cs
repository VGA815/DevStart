namespace DevStart.Application.Abstractions.Notifications
{
    public interface ICentrifugoTokenProvider
    {
        string CreateConnectionToken(Guid userId);
    }
}
