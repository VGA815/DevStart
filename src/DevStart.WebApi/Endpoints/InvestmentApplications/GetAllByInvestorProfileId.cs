using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.GetAllByInvestorProfileId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class GetAllByInvestorProfileId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/investor-profiles/{userId:guid}/investment-applications", async (
                Guid userId,
                IQueryHandler<GetInvestmentApplicationsByInvestorProfileIdQuery, List<InvestmentApplicationResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetInvestmentApplicationsByInvestorProfileIdQuery(userId);
                Result<List<InvestmentApplicationResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsRead)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
