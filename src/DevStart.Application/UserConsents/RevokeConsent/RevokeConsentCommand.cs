using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.UserConsents;

namespace DevStart.Application.UserConsents.RevokeConsent
{
    public sealed record RevokeConsentCommand(ConsentType ConsentType) : ICommand;
}
