namespace DevStart.Application.ExpertCollaborationRequests
{
    /// <summary>
    /// Bound from the "ExpertCollaboration" configuration section. Both windows bound how long a
    /// single expert/startup pair can occupy each other's inbox: unanswered requests time out, and a
    /// rejected side has to wait before asking again.
    /// </summary>
    public sealed class ExpertCollaborationOptions
    {
        /// <summary>
        /// Days a Pending request survives before the expiry job times it out. Zero or less disables
        /// expiry entirely.
        /// </summary>
        public int PendingTtlDays { get; set; } = 30;

        /// <summary>
        /// Days the rejected side must wait before opening another request to the same counterparty.
        /// Only the side whose request was rejected is held back — the side that did the rejecting can
        /// change its mind at any time. Zero or less disables the cooldown.
        /// </summary>
        public int RejectionCooldownDays { get; set; } = 14;
    }
}
