using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Startups.GetById
{
    public sealed record GetStartupByIdQuery(Guid StartupId) : IQuery<StartupResponse>;
}
