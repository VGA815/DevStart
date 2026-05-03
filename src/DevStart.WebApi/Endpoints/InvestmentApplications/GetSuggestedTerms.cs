using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.SuggestedTerms;
using DevStart.Domain.InvestmentApplications;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class GetSuggestedTerms : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/suggested-terms", async (
                Guid startupId,
                InvestmentInstrument instrument,
                decimal amount,
                IQueryHandler<GetSuggestedTermsQuery, SuggestedTermsResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetSuggestedTermsQuery(startupId, instrument, amount);
                Result<SuggestedTermsResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsCreate)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
