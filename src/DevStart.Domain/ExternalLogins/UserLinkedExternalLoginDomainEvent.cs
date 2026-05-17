using DevStart.SharedKernel;

namespace DevStart.Domain.ExternalLogins
{
    public sealed record UserLinkedExternalLoginDomainEvent(
        Guid UserId,
        ExternalLoginProvider Provider,
        string ProviderUserId) : IDomainEvent;
}
