using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.PatentRegistry.RunPatentRegistryImport
{
    /// <summary>
    /// Runs the register refresh now instead of waiting for the quarter — needed before the first
    /// scheduled run ever fires, and whenever a corrected dump or an outage needs re-checking without
    /// a three-month wait.
    /// </summary>
    public sealed record RunPatentRegistryImportCommand : ICommand;
}
