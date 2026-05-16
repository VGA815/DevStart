using DevStart.Application.Abstractions.Data;
using DevStart.Domain.UserConsents;
using Microsoft.EntityFrameworkCore;

namespace DevStart.WebApi.Endpoints.Consents
{
    internal sealed class GetConsentVersions : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/consents/versions", async (
                IApplicationDbContext context,
                CancellationToken cancellationToken) =>
            {
                var activeVersions = await context.ConsentDocuments
                    .Where(d => d.IsActive)
                    .Select(d => new { d.Type, d.Version })
                    .ToListAsync(cancellationToken);

                var versionMap = activeVersions.ToDictionary(x => x.Type, x => x.Version);

                var response = new
                {
                    personal_data_processing = versionMap.GetValueOrDefault(ConsentType.PersonalDataProcessing),
                    privacy_policy           = versionMap.GetValueOrDefault(ConsentType.PrivacyPolicy),
                    terms_of_service         = versionMap.GetValueOrDefault(ConsentType.TermsOfService),
                    cookies                  = versionMap.GetValueOrDefault(ConsentType.Cookies)
                };

                return Results.Ok(response);
            })
            .WithTags(Tags.Consents)
            .AllowAnonymous();
        }
    }
}
