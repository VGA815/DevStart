using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.InvestmentApplications.Create
{
    public sealed class CreateInvestmentApplicationCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }
        public Guid? RoadmapItemId { get; set; }
        public decimal Amount { get; set; }
        public string? Message { get; set; }

        public CreateInvestmentApplicationCommand(Guid startupId, Guid? roadmapItemId, decimal amount, string? message)
        {
            StartupId = startupId;
            RoadmapItemId = roadmapItemId;
            Amount = amount;
            Message = message;
        }
    }
}
