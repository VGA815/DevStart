using DevStart.Application.Users.Register;
using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;

namespace DevStart.Application.UserConsents
{
    public sealed record RequiredConsent(ConsentType Type, string Version, bool Mandatory);

    public interface IConsentService
    {
        /// <summary>
        /// Validates that the submitted consents match the active document versions and that every
        /// mandatory consent is accepted, then returns the <see cref="UserConsent"/> rows to persist.
        /// </summary>
        Task<Result<List<UserConsent>>> BuildAcceptedConsentsAsync(
            Guid userId,
            IReadOnlyList<ConsentItem> consents,
            DateTime now,
            CancellationToken cancellationToken);

        /// <summary>
        /// Returns true when the user has an active consent at the currently active version for every
        /// mandatory consent type.
        /// </summary>
        Task<bool> AreMandatoryConsentsCurrentAsync(Guid userId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the active consent documents (type + version) the client must present for acceptance.
        /// </summary>
        Task<IReadOnlyList<RequiredConsent>> GetRequiredConsentsAsync(CancellationToken cancellationToken);
    }
}
