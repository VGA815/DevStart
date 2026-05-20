using DevStart.SharedKernel;

namespace DevStart.Domain.ExpertCollaborationRequests
{
    public sealed record ExpertCollaborationRequestCreatedDomainEvent(
        Guid RequestId,
        Guid ExpertProfileId,
        Guid StartupId,
        CollaborationType CollaborationType) : IDomainEvent;
}
