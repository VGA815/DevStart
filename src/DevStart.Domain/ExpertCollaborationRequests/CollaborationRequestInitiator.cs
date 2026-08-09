namespace DevStart.Domain.ExpertCollaborationRequests
{
    /// <summary>
    /// Which side opened the request. The initiator may withdraw it; the opposite side accepts or
    /// rejects it. Every other rule (one pending pair, cooldown, expiry) is direction-agnostic.
    /// </summary>
    public enum CollaborationRequestInitiator
    {
        Expert = 0,
        Startup = 1
    }
}
