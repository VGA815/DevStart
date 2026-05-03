namespace DevStart.Application.DealDocuments.Generation
{
    public sealed record EquityHolderInput(
        Guid? PartyId,
        string Name,
        string Type,
        decimal SharePct);
}
