using DevStart.SharedKernel;

namespace DevStart.Domain.Users
{
    public sealed record UserBannedDomainEvent(
        Guid UserId,
        string Reason,
        DateTime? ExpiresAt) : IDomainEvent;
}
