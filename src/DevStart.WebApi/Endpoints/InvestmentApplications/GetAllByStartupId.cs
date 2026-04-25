using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/investment-applications", async (
                Guid startupId,
                IQueryHandler<GetInvestmentApplicationsByStartupIdQuery, List<InvestmentApplicationResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetInvestmentApplicationsByStartupIdQuery(startupId);
                Result<List<InvestmentApplicationResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsRead)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
