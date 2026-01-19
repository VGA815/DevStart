using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupInvestors.ChangeVisibility
{
    public sealed class ChangeStartupInvestorVisibilityCommand : ICommand
    {
        public Guid StartupId { get; set; }
        public bool IsPublic { get; set; }

        public ChangeStartupInvestorVisibilityCommand(Guid startupId, bool isPublic)
        {
            StartupId = startupId;
            IsPublic = isPublic;
        }
    }
}
