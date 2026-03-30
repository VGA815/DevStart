using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.Startups;

namespace DevStart.Application.Startups.GetAll
{
    public sealed record GetStartupsQuery(
        int PageNumber,
        int PageSize,
        StartupStage? Stage = null,
        StartupLocation? Location = null,
        bool? IsStopped = null) : IQuery<List<StartupResponse>>;
}
