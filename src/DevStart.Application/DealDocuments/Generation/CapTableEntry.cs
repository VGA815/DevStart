namespace DevStart.Application.DealDocuments.Generation
{
    public sealed record CapTableEntry(
        Guid? PartyId,
        string PartyName,
        string PartyType,
        decimal SharePctBefore,
        decimal SharePctAfter);
}
