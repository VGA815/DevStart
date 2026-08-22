using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupPartnerships;

namespace DevStart.Application.StartupPartnerships.Update
{
    public sealed class UpdateStartupPartnershipCommand : ICommand
    {
        public Guid PartnershipId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string Website { get; set; } = null!;
        public PartnershipKind Kind { get; set; }
        public string? Description { get; set; }

        public UpdateStartupPartnershipCommand(
            Guid partnershipId, string partnerName, string website, PartnershipKind kind, string? description)
        {
            PartnershipId = partnershipId;
            PartnerName = partnerName;
            Website = website;
            Kind = kind;
            Description = description;
        }
    }
}
