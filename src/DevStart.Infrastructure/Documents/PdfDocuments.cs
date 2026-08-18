using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace DevStart.Infrastructure.Documents
{
    /// <summary>
    /// One-time setup shared by every PDF the platform renders, plus the metadata that makes the
    /// output reproducible.
    /// </summary>
    internal static class PdfDocuments
    {
        /// <summary>Body face: a serif designed for Cyrillic, so the document looks like a document.</summary>
        internal const string SerifFamily = "PT Serif";

        /// <summary>Used for table headings, labels and the running footer.</summary>
        internal const string SansFamily = "PT Sans";

        private static readonly string[] FontResources =
        [
            "PT_Serif-Web-Regular.ttf",
            "PT_Serif-Web-Bold.ttf",
            "PT_Serif-Web-Italic.ttf",
            "PT_Sans-Web-Regular.ttf",
            "PT_Sans-Web-Bold.ttf",
        ];

        private static readonly Lock InitLock = new();
        private static bool _initialized;

        /// <summary>
        /// Registers the licence and the embedded fonts. Idempotent and safe to call from every
        /// render — Hangfire workers run concurrently.
        /// <para>
        /// The fonts ship inside the assembly rather than being looked up on the machine. QuestPDF
        /// does not fall back to system fonts for a family it does not know, and a container image
        /// carries no Cyrillic face at all; embedding removes the entire class of "renders fine
        /// locally, renders boxes in production" failures.
        /// </para>
        /// </summary>
        internal static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            lock (InitLock)
            {
                if (_initialized)
                {
                    return;
                }

                QuestPDF.Settings.License = LicenseType.Community;

                Assembly assembly = typeof(PdfDocuments).Assembly;
                string prefix = $"{assembly.GetName().Name}.Documents.Fonts.";
                foreach (string resource in FontResources)
                {
                    using Stream stream = assembly.GetManifestResourceStream(prefix + resource)
                        ?? throw new InvalidOperationException($"Embedded font not found: {prefix}{resource}");
                    FontManager.RegisterFont(stream);
                }

                _initialized = true;
            }
        }

        /// <summary>
        /// Metadata pinned to constants and to the document's own generation time.
        /// <para>
        /// Left to itself QuestPDF stamps the current clock into the creation and modification dates,
        /// which makes the bytes different on every run and the file's hash meaningless as a
        /// fingerprint of its content. Both dates therefore come from <paramref name="generatedAt"/> —
        /// the value already stored alongside the document — and the remaining fields are fixed
        /// strings.
        /// </para>
        /// </summary>
        internal static DocumentMetadata Metadata(string title, DateTime generatedAt) => new()
        {
            Title = title,
            Author = "DevStart",
            Subject = title,
            Keywords = string.Empty,
            Creator = "DevStart",
            Producer = "DevStart",
            CreationDate = generatedAt,
            ModifiedDate = generatedAt,
        };

        /// <summary>
        /// Renders to bytes with the output-shaping settings pinned.
        /// <para>
        /// Every renderer goes through here rather than calling <c>GeneratePdf</c> itself. The
        /// metadata already fixes the clock out of the file; these settings fix the rest of what a
        /// QuestPDF upgrade could quietly change about the bytes. Determinism within a run is what the
        /// stored hash depends on, and it is tested — but a document re-rendered after an upgrade
        /// should not acquire a different hash for no reason either.
        /// </para>
        /// </summary>
        internal static byte[] ToBytes(Document document)
        {
            EnsureInitialized();

            return document
                .WithSettings(new DocumentSettings
                {
                    CompressDocument = true,
                    ImageCompressionQuality = ImageCompressionQuality.High,
                    ImageRasterDpi = 300,
                    ContentDirection = ContentDirection.LeftToRight,
                    PdfA = false,
                })
                .GeneratePdf();
        }
    }
}
