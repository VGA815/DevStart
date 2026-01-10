using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupFollowers.Create
{
    public sealed class CreateStartupFollowerCommand : ICommand<(Guid startupId, Guid profileId)>
    {
        public Guid StartupId { get; set; }
        public Guid ProfileId { get; set; }
    }
}
