using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupCompetitors.GetAllByStartupId
{
    public sealed class GetStartupCompetitorsByStartupIdQuery : IQuery<List<StartupCompetitorResponse>>
    {
        public Guid StartupId { get; set; }

        public GetStartupCompetitorsByStartupIdQuery(Guid startupId)
        {
            StartupId = startupId;
        }
    }
}
