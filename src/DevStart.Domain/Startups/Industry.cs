namespace DevStart.Domain.Startups
{
    /// <summary>
    /// Sector of a startup. Drives sector-specific valuation constants (Scorecard medians,
    /// VC Method EV/Revenue exit multiples). <see cref="Other"/> is the default and falls back
    /// to stage-only constants in the valuation engine.
    /// </summary>
    public enum Industry
    {
        Other = 0,
        Saas = 1,
        Fintech = 2,
        Ai = 3,
        Ecommerce = 4,
        Marketplace = 5,
        Hardware = 6,
        Biotech = 7,
        Edtech = 8,
    }
}
