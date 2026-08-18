using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupPatents.Delete
{
    public sealed class DeleteStartupPatentCommand : ICommand
    {
        public Guid PatentId { get; set; }

        public DeleteStartupPatentCommand(Guid patentId)
        {
            PatentId = patentId;
        }
    }
}
