namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// MOEX ISS access. The base URL is configurable so an integration test can point the collector at
    /// a stub server instead of the real exchange — without that the job is untestable end to end.
    /// </summary>
    public sealed class MoexOptions
    {
        public const string SectionName = "Moex";

        public string BaseUrl { get; set; } = "https://iss.moex.com";

        /// <summary>Trading board the quotes are read from. TQBR is the main T+ board for shares.</summary>
        public string Board { get; set; } = "TQBR";

        public int TimeoutSeconds { get; set; } = 20;
    }
}
