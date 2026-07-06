namespace DevStart.Application.DealDocuments.Generation
{
    /// <summary>
    /// A row of the computed post-deal cap table. <see cref="VestedPctAfter"/> is the portion of
    /// <see cref="SharePctAfter"/> that has vested as of the generation date (equals
    /// <see cref="SharePctAfter"/> when the holder is fully vested or has no schedule).
    /// </summary>
    public sealed record CapTableEntry(
        Guid? PartyId,
        string PartyName,
        string PartyType,
        decimal SharePctBefore,
        decimal SharePctAfter,
        decimal VestedPctAfter = 0m);
}
