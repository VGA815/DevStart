using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.StartupPatents;

namespace DevStart.Application.Admin.PatentRegistry.UploadPatentRegistryDataset
{
    /// <summary>
    /// Loads one Rospatent open-data dump by hand. The quarterly job is the normal path; this is the
    /// same load without waiting for it — for the first fill, for a corrected file, or when the
    /// published URL is not reachable from the server.
    ///
    /// <paramref name="Kind"/> is stated rather than guessed: open data ships one register per file and
    /// the file itself does not say which. A guess that is wrong here would file trademark numbers as
    /// patents — a confident, false answer, which is worse than asking.
    /// </summary>
    public sealed record UploadPatentRegistryDatasetCommand(
        Stream Content,
        long Length,
        string FileName,
        string? ContentType,
        IntellectualPropertyKind Kind) : ICommand<UploadPatentRegistryDatasetResponse>
    {
        /// <summary>
        /// Generous for a slice of a register, deliberately not enough for the whole thing: a full
        /// dump belongs on the configured URL, where the job streams it, instead of through a request
        /// body that has to be buffered. Checked on the stated length before a byte is read.
        /// </summary>
        public const long MaxLengthBytes = 64L * 1024 * 1024;
    }

    public sealed class UploadPatentRegistryDatasetResponse
    {
        public IntellectualPropertyKind Kind { get; init; }

        /// <summary>Rows the parser accepted.</summary>
        public int RecordsParsed { get; init; }

        public int Inserted { get; init; }

        public int Updated { get; init; }

        /// <summary>Rows dropped on their own merits — unusable number, or a shape that is not this kind.</summary>
        public int SkippedRows { get; init; }
    }
}
