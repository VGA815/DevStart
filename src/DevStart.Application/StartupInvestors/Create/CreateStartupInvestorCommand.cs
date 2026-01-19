using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupInvestors.Create
{
    public sealed class CreateStartupInvestorCommand : ICommand<(Guid startupId, Guid profileId)>
    {
        public Guid StartupId { get; set; }
        public Guid ProfileId { get; set; }
        public bool IsPublic { get; set; }

        public CreateStartupInvestorCommand(Guid startupId, Guid profileId, bool isPublic)
        {
            StartupId = startupId;
            ProfileId = profileId;
            IsPublic = isPublic;
        }
    }
}
