using System.IO.Compression;
using System.Text;
using DevStart.Application.PatentRegistry;
using Microsoft.Extensions.Options;

namespace DevStart.Infrastructure.PatentRegistry
{
    /// <summary>
    /// Fetches one open-data dump and hands back its CSV text. Deliberately thin: it downloads, unzips
    /// if needed, and decodes — every decision about what the rows mean belongs to the parser.
    /// </summary>
    // Public for the same reason as MoexIssClient: its consumer is a Hangfire job.
    public sealed class RospatentDumpClient(HttpClient httpClient, IOptions<RospatentOptions> options)
    {
        private readonly RospatentOptions _options = options.Value;

        /// <summary>
        /// Downloads and decodes a dump. Throws <see cref="InvalidDataException"/> with a readable
        /// reason when the file is too large, is not UTF-8, or holds no CSV — the caller isolates the
        /// failure to one register and leaves the other loads and the previous data alone.
        /// </summary>
        public async Task<string> DownloadCsvAsync(string url, CancellationToken cancellationToken)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(
                url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength > _options.MaxDatasetBytes)
            {
                throw TooLarge(_options.MaxDatasetBytes);
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            byte[] bytes = await CappedStreamReader.ReadAsync(stream, _options.MaxDatasetBytes, cancellationToken);

            return Decode(IsZip(bytes)
                ? await ExtractCsvAsync(bytes, cancellationToken)
                : bytes);
        }

        private static bool IsZip(byte[] bytes) =>
            bytes.Length >= 2 && bytes[0] == 'P' && bytes[1] == 'K';

        /// <summary>
        /// Reads the first CSV out of the archive, under the same cap as a plain download.
        ///
        /// The cap is the point. A 200 KB archive can expand into tens of gigabytes — a zip bomb needs
        /// no exploit, only a very compressible file — and an uncapped copy would hand the server's
        /// memory to whoever controls the configured URL. The declared entry size is checked first
        /// because it costs nothing, and the copy is capped anyway: that size comes from the archive's
        /// own central directory, so it is a hint, not evidence.
        /// </summary>
        private async Task<byte[]> ExtractCsvAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);

            ZipArchiveEntry entry = archive.Entries
                .FirstOrDefault(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidDataException("В архиве нет ни одного CSV-файла.");

            if (entry.Length > _options.MaxDatasetBytes)
            {
                throw TooLarge(_options.MaxDatasetBytes);
            }

            await using Stream entryStream = entry.Open();
            return await CappedStreamReader.ReadAsync(entryStream, _options.MaxDatasetBytes, cancellationToken);
        }

        private static InvalidDataException TooLarge(int cap) =>
            new(CappedStreamReader.TooLargeMessage(cap));

        /// <summary>
        /// UTF-8 only, and strictly. Russian exports are often windows-1251, and a lenient decode would
        /// turn every Cyrillic holder name into replacement characters that look like data — better to
        /// refuse and say so, the same way the Damodaran import refuses a layout it cannot read.
        /// </summary>
        private static string Decode(byte[] bytes)
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

            try
            {
                return utf8.GetString(bytes);
            }
            catch (DecoderFallbackException exception)
            {
                throw new InvalidDataException(
                    "Выгрузка не в UTF-8 (вероятно windows-1251) — сконвертируйте файл перед загрузкой.",
                    exception);
            }
        }
    }
}
