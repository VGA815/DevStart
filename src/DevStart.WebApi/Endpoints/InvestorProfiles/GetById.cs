using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestorProfiles.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using System.Security.Claims;

namespace DevStart.WebApi.Endpoints.InvestorProfiles
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            // Anonymous, like the catalog that links here: requiring a permission made every logged-out
            // click on a listed investor look like "no such investor". The handler still hides a
            // non-public profile from everyone but its owner, which is the same line the catalog draws.
            app.MapGet("api/investor-profiles/{userId:guid}", async (
                Guid userId,
                ClaimsPrincipal user,
                IQueryHandler<GetInvestorProfileByIdQuery, InvestorProfileResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Guid? viewerId = Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id)
                    ? id
                    : null;

                var query = new GetInvestorProfileByIdQuery(userId, viewerId);
                Result<InvestorProfileResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.InvestorProfiles);
        }
    }
}
