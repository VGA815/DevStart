namespace DevStart.Infrastructure.PatentRegistry
{
    /// <summary>
    /// Where the quarterly load reads its dumps from. Open data rather than an API on purpose: the
    /// deployment is one server, and putting the availability of a startup card behind the
    /// availability of Rospatent buys nothing. A dump gives zero runtime dependency — the external
    /// service is needed only while refreshing.
    ///
    /// No URL configured for a kind means the job does not load it; that kind then reports "проверка
    /// недоступна" rather than "не найдено", which is the honest reading (SC-64).
    /// </summary>
    public sealed class RospatentOptions
    {
        public const string SectionName = "Rospatent";

        /// <summary>
        /// Dump URL per <see cref="Domain.StartupPatents.IntellectualPropertyKind"/> name, e.g.
        /// <c>Rospatent:DatasetUrls:ComputerProgram</c>. CSV, or a ZIP holding one.
        /// </summary>
        public Dictionary<string, string> DatasetUrls { get; set; } = [];

        /// <summary>A whole register is a large file; the download is generous but not unbounded.</summary>
        public int MaxDatasetBytes { get; set; } = 512 * 1024 * 1024;

        public int TimeoutSeconds { get; set; } = 600;
    }
}
