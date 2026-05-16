using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.UserConsents.GetConsents;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Consents
{
    internal sealed class GetUserConsents : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/consents", async (
                IQueryHandler<GetUserConsentsQuery, List<UserConsentResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserConsentsQuery();

                Result<List<UserConsentResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .HasPermission(Permissions.ConsentsRead)
            .WithTags(Tags.Consents);
        }
    }
}
