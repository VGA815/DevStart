using DevStart.SharedKernel;

namespace DevStart.Domain.AccountDeletion
{
    public static class AccountDeletionErrors
    {
        public static readonly Error AlreadyRequested = Error.Conflict(
            "AccountDeletion.AlreadyRequested",
            "Account deletion has already been requested");

        public static readonly Error NotRequested = Error.NotFound(
            "AccountDeletion.NotRequested",
            "There is no pending account deletion request");

        public static readonly Error NotPending = Error.Conflict(
            "AccountDeletion.NotPending",
            "The account deletion request is no longer pending");

        // An admin erasing themselves would drop the permissions the platform is moderated with, and
        // (for the last admin) leave nobody able to lift bans or activate consent documents. The way
        // out is to have another admin demote the account first.
        public static readonly Error AdminCannotSelfDelete = Error.Conflict(
            "AccountDeletion.AdminCannotSelfDelete",
            "An administrator account cannot be deleted through self-service. Ask another administrator to change the role first");
    }
}
