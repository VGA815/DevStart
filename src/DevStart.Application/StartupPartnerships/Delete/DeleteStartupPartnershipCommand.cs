using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupPartnerships.Delete
{
    public sealed class DeleteStartupPartnershipCommand : ICommand
    {
        public Guid PartnershipId { get; set; }

        public DeleteStartupPartnershipCommand(Guid partnershipId)
        {
            PartnershipId = partnershipId;
        }
    }
}
