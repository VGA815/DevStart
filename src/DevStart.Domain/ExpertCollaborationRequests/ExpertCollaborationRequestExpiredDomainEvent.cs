using DevStart.SharedKernel;

namespace DevStart.Domain.ExpertCollaborationRequests
{
    public sealed record ExpertCollaborationRequestExpiredDomainEvent(
        Guid RequestId,
        Guid ExpertProfileId,
        Guid StartupId,
        CollaborationRequestInitiator Initiator) : IDomainEvent;
}
