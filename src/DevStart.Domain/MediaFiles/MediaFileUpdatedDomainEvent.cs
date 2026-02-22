using DevStart.SharedKernel;

namespace DevStart.Domain.MediaFiles
{
    public sealed record MediaFileUpdatedDomainEvent(Guid MediaFileId) : IDomainEvent;
}
