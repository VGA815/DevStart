using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupPartnerships;

namespace DevStart.Application.StartupPartnerships.Create
{
    public sealed class CreateStartupPartnershipCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string Website { get; set; } = null!;
        public PartnershipKind Kind { get; set; }

        /// <summary>
        /// What the arrangement is and what it gives the startup. Optional — a record without it is
        /// listed but is not counted as worked out, so it is worth nothing to the valuation.
        /// </summary>
        public string? Description { get; set; }

        public CreateStartupPartnershipCommand(
            Guid startupId, string partnerName, string website, PartnershipKind kind, string? description)
        {
            StartupId = startupId;
            PartnerName = partnerName;
            Website = website;
            Kind = kind;
            Description = description;
        }
    }
}
