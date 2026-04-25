using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestorProfiles.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestorProfiles
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/investor-profiles/{userId:guid}", async (
                Guid userId,
                IQueryHandler<GetInvestorProfileByIdQuery, InvestorProfileResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetInvestorProfileByIdQuery(userId);
                Result<InvestorProfileResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestorProfilesRead)
                .WithTags(Tags.InvestorProfiles);
        }
    }
}
