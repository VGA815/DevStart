namespace DevStart.Domain.StartupEquity
{
    /// <summary>
    /// Kind of holder on a startup's founding (pre-money) cap table. Persisted as <c>int</c>.
    /// The name is also surfaced as the cap-table "party type" string in generated documents.
    /// </summary>
    public enum EquityHolderType
    {
        Founder = 0,
        Esop = 1,
        Advisor = 2,
        Other = 3,
    }
}
