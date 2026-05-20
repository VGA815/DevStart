using DevStart.SharedKernel;

namespace DevStart.Domain.ExpertCollaborationRequests
{
    public sealed record ExpertCollaborationRequestAcceptedDomainEvent(
        Guid RequestId,
        Guid ExpertProfileId,
        Guid StartupId) : IDomainEvent;
}
