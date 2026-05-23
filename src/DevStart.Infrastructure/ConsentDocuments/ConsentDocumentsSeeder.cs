using DevStart.Application.Abstractions.Data;
using DevStart.Application.UserConsents;
using DevStart.Domain.ConsentDocuments;
using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace DevStart.Infrastructure.ConsentDocuments
{
    /// <summary>
    /// On startup, seeds the initial consent document texts from embedded Markdown resources.
    /// Idempotent: skips documents whose (type, version) already exist in the database.
    /// If no active document exists for a type, the seeded document is automatically activated.
    /// </summary>
    internal sealed class ConsentDocumentsSeeder(
        IServiceProvider serviceProvider,
        IDateTimeProvider dateTimeProvider,
        ILogger<ConsentDocumentsSeeder> logger) : IHostedService
    {
        private static readonly (string ResourceSuffix, ConsentType Type, string Version, string Title)[] SeedDocuments =
        [
            (
                "ConsentDocuments.Documents.personal-data-processing-v1.0.md",
                ConsentType.PersonalDataProcessing,
                ConsentVersions.PersonalDataProcessing,
                "Согласие на обработку персональных данных"
            ),
            (
                "ConsentDocuments.Documents.privacy-policy-v1.0.md",
                ConsentType.PrivacyPolicy,
                ConsentVersions.PrivacyPolicy,
                "Политика конфиденциальности"
            ),
            (
                "ConsentDocuments.Documents.terms-of-service-v1.0.md",
                ConsentType.TermsOfService,
                ConsentVersions.TermsOfService,
                "Пользовательское соглашение"
            ),
            (
                "ConsentDocuments.Documents.cookies-v1.0.md",
                ConsentType.Cookies,
                ConsentVersions.Cookies,
                "Политика использования Cookie"
            ),
            (
                "ConsentDocuments.Documents.offer-agreement-v1.0.md",
                ConsentType.PublicOffer,
                ConsentVersions.PublicOffer,
                "Публичная оферта"
            ),
        ];

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                IApplicationDbContext context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                Assembly assembly = typeof(ConsentDocumentsSeeder).Assembly;
                string assemblyName = assembly.GetName().Name!;
                DateTime now = dateTimeProvider.UtcNow;

                foreach ((string resourceSuffix, ConsentType type, string version, string title) in SeedDocuments)
                {
                    bool alreadyExists = await context.ConsentDocuments
                        .AnyAsync(d => d.Type == type && d.Version == version, cancellationToken);

                    if (alreadyExists)
                    {
                        logger.LogDebug(
                            "Consent document {Type} v{Version} already exists, skipping",
                            type, version);
                        continue;
                    }

                    string fullName = $"{assemblyName}.{resourceSuffix}";
                    using Stream? resource = assembly.GetManifestResourceStream(fullName);

                    if (resource is null)
                    {
                        logger.LogWarning(
                            "Consent document embedded resource not found: {Resource}", fullName);
                        continue;
                    }

                    using var reader = new StreamReader(resource);
                    string content = await reader.ReadToEndAsync(cancellationToken);

                    ConsentDocument document = ConsentDocument.Create(type, version, title, content, now);

                    // Activate if there is no active document for this type yet
                    bool hasActiveDocument = await context.ConsentDocuments
                        .AnyAsync(d => d.Type == type && d.IsActive, cancellationToken);

                    if (!hasActiveDocument)
                    {
                        document.Activate();
                    }

                    context.ConsentDocuments.Add(document);
                    await context.SaveChangesAsync(cancellationToken);

                    logger.LogInformation(
                        "Seeded consent document {Type} v{Version} (IsActive={IsActive})",
                        type, version, document.IsActive);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed consent documents");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
