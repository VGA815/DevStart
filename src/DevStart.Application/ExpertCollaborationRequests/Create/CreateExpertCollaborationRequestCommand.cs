using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ExpertCollaborationRequests;

namespace DevStart.Application.ExpertCollaborationRequests.Create
{
    public sealed class CreateExpertCollaborationRequestCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }

        /// <summary>
        /// The expert the request concerns. Required when a startup invites an expert; optional (and,
        /// when present, must be the caller) when an expert applies to a startup. The handler derives
        /// the direction from the caller's relationship to the startup rather than trusting this field.
        /// </summary>
        public Guid? ExpertProfileId { get; set; }

        public CollaborationType CollaborationType { get; set; }
        public string? Message { get; set; }
        public int? ProposedHoursPerWeek { get; set; }
        public decimal? ProposedRate { get; set; }

        public CreateExpertCollaborationRequestCommand(
            Guid startupId,
            Guid? expertProfileId,
            CollaborationType collaborationType,
            string? message,
            int? proposedHoursPerWeek,
            decimal? proposedRate)
        {
            StartupId = startupId;
            ExpertProfileId = expertProfileId;
            CollaborationType = collaborationType;
            Message = message;
            ProposedHoursPerWeek = proposedHoursPerWeek;
            ProposedRate = proposedRate;
        }
    }
}
