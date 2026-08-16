namespace DevStart.Domain.AccountDeletion
{
    public enum AccountDeletionRequestStatus
    {
        /// <summary>Waiting out the grace window; the user may still cancel.</summary>
        Pending = 0,

        /// <summary>Withdrawn by the user before the window elapsed.</summary>
        Cancelled = 1,

        /// <summary>Erasure has run. The row outlives the account as proof of when it happened.</summary>
        Completed = 2,
    }
}
