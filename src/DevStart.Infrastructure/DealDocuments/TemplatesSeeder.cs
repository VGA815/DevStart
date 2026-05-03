using DevStart.Application.Abstractions.Data;
using DevStart.Application.DealDocuments.Generation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DevStart.Infrastructure.DealDocuments
{
    /// <summary>
    /// On startup, uploads the bundled term-sheet markdown templates to the MinIO `templates`
    /// bucket. Idempotent: existing files are overwritten with the latest version each boot,
    /// which keeps deployments simple (templates ship with code).
    /// </summary>
    internal sealed class TemplatesSeeder(
        IServiceProvider serviceProvider,
        ILogger<TemplatesSeeder> logger) : IHostedService
    {
        private static readonly (string ResourceSuffix, string TemplateKey)[] Templates =
        {
            ("DealDocuments.Templates.term-sheet-safe.md", "term-sheet-safe.md"),
            ("DealDocuments.Templates.term-sheet-convertible.md", "term-sheet-convertible.md"),
            ("DealDocuments.Templates.term-sheet-priced.md", "term-sheet-priced.md"),
        };

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                IFileStorage fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

                Assembly assembly = typeof(TemplatesSeeder).Assembly;
                string assemblyName = assembly.GetName().Name!;

                foreach ((string resourceSuffix, string templateKey) in Templates)
                {
                    string fullName = $"{assemblyName}.{resourceSuffix}";
                    using Stream? source = assembly.GetManifestResourceStream(fullName);
                    if (source is null)
                    {
                        logger.LogWarning("Template embedded resource not found: {Resource}", fullName);
                        continue;
                    }

                    using var ms = new MemoryStream();
                    await source.CopyToAsync(ms, cancellationToken);
                    ms.Position = 0;

                    await fileStorage.UploadAsync(
                        templateKey,
                        ms,
                        DealDocumentBuckets.Templates,
                        "text/markdown; charset=utf-8",
                        cancellationToken);

                    logger.LogInformation("Uploaded template {Template} to bucket {Bucket}",
                        templateKey, DealDocumentBuckets.Templates);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed term sheet templates");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
