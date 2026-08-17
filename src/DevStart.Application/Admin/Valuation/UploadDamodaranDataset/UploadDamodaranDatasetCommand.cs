using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.Admin.Valuation.UploadDamodaranDataset
{
    /// <summary>
    /// Imports one annual Damodaran industry dataset. The file changes once a year, so there is no
    /// fetcher — an admin uploads it.
    ///
    /// <paramref name="DatasetYear"/> and <paramref name="DatasetRegion"/> are stated by hand rather than
    /// guessed from the file. Both travel into the derived benchmark's source string, and a guess that
    /// is wrong there is worse than no automation at all: the number would carry a confident, false
    /// provenance.
    /// </summary>
    public sealed record UploadDamodaranDatasetCommand(
        Stream Content,
        long Length,
        string FileName,
        string ContentType,
        int DatasetYear,
        string DatasetRegion) : ICommand<UploadDamodaranDatasetResponse>
    {
        /// <summary>
        /// A Damodaran industry sheet is a hundred-odd rows — tens of kilobytes. The cap is generous by
        /// two orders of magnitude and still keeps an authenticated admin from buffering an arbitrary
        /// body into memory. Checked before a single byte is read.
        /// </summary>
        public const long MaxLengthBytes = 5 * 1024 * 1024;
    }

    /// <summary>Outcome of an import: what landed, and what work it created.</summary>
    public sealed class UploadDamodaranDatasetResponse
    {
        public int BucketsImported { get; init; }

        /// <summary>Buckets with no mapping row — the SC-58 queue this upload just added to.</summary>
        public int UnmappedBuckets { get; init; }

        /// <summary>MinIO key of the stored original: the artefact of provenance for this import.</summary>
        public string ObjectKey { get; init; } = null!;
    }
}
