using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.UserConsents.GetConsents
{
    public sealed record GetUserConsentsQuery : IQuery<List<UserConsentResponse>>;
}
