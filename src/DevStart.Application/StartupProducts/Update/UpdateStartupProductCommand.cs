using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupProducts.Update
{
    public sealed class UpdateStartupProductCommand : ICommand
    {
        public Guid StartupId { get; init; }
        public string Problem { get; init; } = null!;
        public string Solution { get; init; } = null!;
        public List<string> Stack { get; init; } = [];
        public string ValueProposition { get; init; } = null!;
        public string Differentiators { get; init; } = null!;

        public UpdateStartupProductCommand(Guid startupId, string problem, string solution, List<string> stack, string valueProposition, string differentiators)
        {
            StartupId = startupId;
            Problem = problem;
            Solution = solution;
            Stack = stack;
            ValueProposition = valueProposition;
            Differentiators = differentiators;
        }
    }
}
