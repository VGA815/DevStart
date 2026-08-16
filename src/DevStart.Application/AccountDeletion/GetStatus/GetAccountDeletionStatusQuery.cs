using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.AccountDeletion.GetStatus
{
    public sealed record GetAccountDeletionStatusQuery : IQuery<AccountDeletionStatusResponse>;

    /// <summary>
    /// What deleting this account would cost, and — once requested — when it happens. Deliberately
    /// answerable before anything is requested: the startups list is the warning the user needs
    /// <em>before</em> pressing the button, not after.
    /// </summary>
    public sealed record AccountDeletionStatusResponse(
        bool Pending,
        DateTime? RequestedAt,
        DateTime? ScheduledFor,
        IReadOnlyList<AffectedStartupResponse> StartupsToDelete);

    public sealed record AffectedStartupResponse(Guid Id, string Name);
}
