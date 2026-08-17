namespace DevStart.Infrastructure.Valuation
{
    /// <summary>
    /// ГИР БО (bo.nalog.gov.ru) access. Base URL configurable for the same reason as
    /// <see cref="MoexOptions"/>: the integration test points the collector at a stub.
    /// </summary>
    public sealed class GirBoOptions
    {
        public const string SectionName = "GirBo";

        public string BaseUrl { get; set; } = "https://bo.nalog.gov.ru";

        public int TimeoutSeconds { get; set; } = 30;
    }
}
