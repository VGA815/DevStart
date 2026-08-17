namespace DevStart.Domain.Valuation
{
    /// <summary>Which external taxonomy a <see cref="BenchmarkIndustryMapping.ExternalKey"/> belongs to.</summary>
    public enum BenchmarkMappingSourceKind
    {
        /// <summary>A Damodaran industry bucket name, e.g. "Software (System &amp; Application)".</summary>
        Damodaran = 0,

        /// <summary>An ОКВЭД activity code. Reserved for a future competition-density derivation.</summary>
        Okved = 1,
    }
}
