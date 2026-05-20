using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExpertCollaborationRequests;

namespace DevStart.Application.ExpertCollaborationRequests.Create
{
    public sealed class CreateExpertCollaborationRequestCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }
        public CollaborationType CollaborationType { get; set; }
        public string? Message { get; set; }
        public int? ProposedHoursPerWeek { get; set; }
        public decimal? ProposedRate { get; set; }

        public CreateExpertCollaborationRequestCommand(
            Guid startupId,
            CollaborationType collaborationType,
            string? message,
            int? proposedHoursPerWeek,
            decimal? proposedRate)
        {
            StartupId = startupId;
            CollaborationType = collaborationType;
            Message = message;
            ProposedHoursPerWeek = proposedHoursPerWeek;
            ProposedRate = proposedRate;
        }
    }
}
