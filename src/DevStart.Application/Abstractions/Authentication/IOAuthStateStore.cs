namespace DevStart.Application.Abstractions.Authentication
{
    public interface IOAuthStateStore
    {
        Task SaveAsync(string state, OAuthStateEntry entry, TimeSpan ttl, CancellationToken cancellationToken);

        Task<OAuthStateEntry?> ConsumeAsync(string state, CancellationToken cancellationToken);
    }
}
