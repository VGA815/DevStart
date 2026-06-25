using System.Text.Json;

namespace DevStart.Application.Scoring
{
    /// <summary>
    /// Pinned serializer options for the persisted valuation breakdown JSON. Both write paths
    /// (term-sheet generation and the on-demand recompute) use this single instance so stored
    /// snapshots stay format-stable and comparable across releases, independent of any future change
    /// to the framework's serializer defaults.
    /// </summary>
    public static class ValuationSnapshotJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = null, // stable PascalCase property names
            WriteIndented = false,
        };
    }
}
