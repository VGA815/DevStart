namespace DevStart.Application.DealDocuments.Generation
{
    /// <summary>
    /// A pre-deal equity holder fed into the cap-table calculator. <see cref="VestedFraction"/> is
    /// the share of this holder's stake that has vested as of the calculation date (1 = fully vested,
    /// the default when no schedule applies).
    /// </summary>
    public sealed record EquityHolderInput(
        Guid? PartyId,
        string Name,
        string Type,
        decimal SharePct,
        decimal VestedFraction = 1m);
}
