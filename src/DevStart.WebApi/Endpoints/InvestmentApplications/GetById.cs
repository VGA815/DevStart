using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/investment-applications/{applicationId:guid}", async (
                Guid applicationId,
                IQueryHandler<GetInvestmentApplicationByIdQuery, InvestmentApplicationResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetInvestmentApplicationByIdQuery(applicationId);
                Result<InvestmentApplicationResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsRead)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
