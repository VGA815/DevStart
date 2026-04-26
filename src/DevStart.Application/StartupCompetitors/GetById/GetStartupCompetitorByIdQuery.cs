using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupCompetitors.GetById
{
    public sealed class GetStartupCompetitorByIdQuery : IQuery<StartupCompetitorResponse>
    {
        public Guid CompetitorId { get; set; }

        public GetStartupCompetitorByIdQuery(Guid competitorId)
        {
            CompetitorId = competitorId;
        }
    }
}
