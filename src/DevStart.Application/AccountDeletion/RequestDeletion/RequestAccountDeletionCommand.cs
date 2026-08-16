using DevStart.Application.AccountDeletion.GetStatus;
using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.AccountDeletion.RequestDeletion
{
    /// <summary>
    /// Schedules the caller's own account for erasure. <paramref name="Password"/> re-confirms the
    /// account holder is present; it is required whenever the account has a password, and impossible
    /// (so not required) for accounts that only ever signed in through Google or GitHub.
    /// </summary>
    public sealed record RequestAccountDeletionCommand(string? Password)
        : ICommand<AccountDeletionStatusResponse>;
}
