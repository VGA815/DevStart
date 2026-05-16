using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.UserConsents.GetConsents
{
    internal sealed class GetUserConsentsQueryHandler(
        IApplicationDbContext context,
        IUserContext userContext)
        : IQueryHandler<GetUserConsentsQuery, List<UserConsentResponse>>
    {
        public async Task<Result<List<UserConsentResponse>>> Handle(
            GetUserConsentsQuery query,
            CancellationToken cancellationToken)
        {
            Guid userId = userContext.UserId;

            // Fetch raw data from DB, then compute IsMandatory in-memory
            var rows = await context.UserConsents
                .Where(uc => uc.UserId == userId)
                .OrderBy(uc => uc.Type)
                .ThenBy(uc => uc.AcceptedAt)
                .Select(uc => new
                {
                    uc.Type,
                    uc.DocumentVersion,
                    uc.AcceptedAt,
                    uc.RevokedAt
                })
                .ToListAsync(cancellationToken);

            List<UserConsentResponse> consents = rows
                .Select(uc => new UserConsentResponse
                {
                    Type            = uc.Type,
                    DocumentVersion = uc.DocumentVersion,
                    AcceptedAt      = uc.AcceptedAt,
                    RevokedAt       = uc.RevokedAt,
                    IsActive        = uc.RevokedAt is null,
                    IsMandatory     = ConsentVersions.MandatoryTypes.Contains(uc.Type)
                })
                .ToList();

            return consents;
        }
    }
}
