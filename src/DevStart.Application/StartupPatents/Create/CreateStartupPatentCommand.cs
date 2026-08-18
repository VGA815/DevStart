using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupPatents;

namespace DevStart.Application.StartupPatents.Create
{
    public sealed class CreateStartupPatentCommand : ICommand<Guid>
    {
        public Guid StartupId { get; set; }

        public IntellectualPropertyKind Kind { get; set; }

        /// <summary>The number as typed. Normalization and the per-kind format check happen server-side.</summary>
        public string Number { get; set; } = null!;

        public CreateStartupPatentCommand(Guid startupId, IntellectualPropertyKind kind, string number)
        {
            StartupId = startupId;
            Kind = kind;
            Number = number;
        }
    }
}
