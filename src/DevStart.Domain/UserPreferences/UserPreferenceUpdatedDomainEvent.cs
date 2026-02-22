using DevStart.SharedKernel;

namespace DevStart.Domain.UserPreferences
{
    public sealed record UserPreferenceUpdatedDomainEvent(Guid UserPreferenceId) : IDomainEvent;
}
