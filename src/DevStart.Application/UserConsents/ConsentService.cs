using DevStart.Application.Abstractions.Data;
using DevStart.Application.Users.Register;
using DevStart.Domain.ConsentDocuments;
using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.UserConsents
{
    internal sealed class ConsentService(IApplicationDbContext context) : IConsentService
    {
        public async Task<Result<List<UserConsent>>> BuildAcceptedConsentsAsync(
            Guid userId,
            IReadOnlyList<ConsentItem> consents,
            DateTime now,
            CancellationToken cancellationToken)
        {
            Dictionary<ConsentType, string> activeVersions = await context.ConsentDocuments
                .Where(d => d.IsActive)
                .ToDictionaryAsync(d => d.Type, d => d.Version, cancellationToken);

            // Every submitted consent must reference the currently active document version.
            foreach (ConsentItem consent in consents)
            {
                if (!activeVersions.TryGetValue(consent.Type, out string? activeVersion))
                {
                    return Result.Failure<List<UserConsent>>(ConsentDocumentErrors.NoActiveDocument(consent.Type));
                }

                if (consent.DocumentVersion != activeVersion)
                {
                    return Result.Failure<List<UserConsent>>(
                        UserConsentErrors.ConsentVersionMismatch(consent.Type, activeVersion));
                }
            }

            // Every mandatory consent must be present, accepted and current.
            foreach (ConsentType mandatory in ConsentVersions.MandatoryTypes)
            {
                bool accepted = consents.Any(c =>
                    c.Type == mandatory
                    && c.Accepted
                    && activeVersions.TryGetValue(mandatory, out string? v)
                    && c.DocumentVersion == v);

                if (!accepted)
                {
                    return Result.Failure<List<UserConsent>>(Error.Problem(
                        "UserConsents.MandatoryConsentRequired",
                        $"The mandatory consent '{mandatory}' must be accepted at its current version."));
                }
            }

            List<UserConsent> result = consents
                .Where(c => c.Accepted)
                .Select(c => UserConsent.Create(userId, c.Type, c.DocumentVersion, now))
                .ToList();

            return result;
        }

        public async Task<bool> AreMandatoryConsentsCurrentAsync(Guid userId, CancellationToken cancellationToken)
        {
            Dictionary<ConsentType, string> activeVersions = await context.ConsentDocuments
                .Where(d => d.IsActive)
                .ToDictionaryAsync(d => d.Type, d => d.Version, cancellationToken);

            List<UserConsent> userConsents = await context.UserConsents
                .Where(c => c.UserId == userId && c.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (ConsentType mandatory in ConsentVersions.MandatoryTypes)
            {
                if (!activeVersions.TryGetValue(mandatory, out string? activeVersion))
                {
                    return false;
                }

                if (!userConsents.Any(c => c.Type == mandatory && c.DocumentVersion == activeVersion))
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<IReadOnlyList<RequiredConsent>> GetRequiredConsentsAsync(CancellationToken cancellationToken)
        {
            var docs = await context.ConsentDocuments
                .Where(d => d.IsActive)
                .Select(d => new { d.Type, d.Version })
                .ToListAsync(cancellationToken);

            return docs
                .Select(d => new RequiredConsent(d.Type, d.Version, ConsentVersions.MandatoryTypes.Contains(d.Type)))
                .ToList();
        }
    }
}
