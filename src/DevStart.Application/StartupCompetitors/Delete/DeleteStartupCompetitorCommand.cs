using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupCompetitors.Delete
{
    public sealed class DeleteStartupCompetitorCommand : ICommand
    {
        public Guid CompetitorId { get; set; }

        public DeleteStartupCompetitorCommand(Guid competitorId)
        {
            CompetitorId = competitorId;
        }
    }
}
