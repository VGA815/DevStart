using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.StartupEquity.SetCapTable
{
    /// <summary>Replaces a startup's entire founding cap table in one atomic operation. The
    /// supplied holders must sum to exactly 100%.</summary>
    public sealed record SetStartupCapTableCommand(
        Guid StartupId,
        IReadOnlyList<CapTableHolderInput> Holders) : ICommand;
}
