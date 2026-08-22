using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupPartnerships.GetAllByStartupId
{
    public sealed class GetStartupPartnershipsByStartupIdQuery : IQuery<List<StartupPartnershipResponse>>
    {
        public Guid StartupId { get; set; }

        public GetStartupPartnershipsByStartupIdQuery(Guid startupId)
        {
            StartupId = startupId;
        }
    }
}
