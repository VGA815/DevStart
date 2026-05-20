using DevStart.SharedKernel;

namespace DevStart.Domain.ExpertCollaborationRequests
{
    public sealed record ExpertCollaborationRequestRejectedDomainEvent(
        Guid RequestId,
        Guid ExpertProfileId,
        Guid StartupId) : IDomainEvent;
}
