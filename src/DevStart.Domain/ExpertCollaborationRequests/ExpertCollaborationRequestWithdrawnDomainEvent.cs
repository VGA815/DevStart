using DevStart.SharedKernel;

namespace DevStart.Domain.ExpertCollaborationRequests
{
    public sealed record ExpertCollaborationRequestWithdrawnDomainEvent(
        Guid RequestId,
        Guid ExpertProfileId,
        Guid StartupId) : IDomainEvent;
}
