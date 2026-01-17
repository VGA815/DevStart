
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.UserPreferences.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.UserPreferences
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("users/preferences/{preferenceId:guid}", async (
                Guid preferenceId, 
                IQueryHandler<GetUserPreferenceByIdQuery, UserPreferenceResponse> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserPreferenceByIdQuery(preferenceId);

                Result<UserPreferenceResponse> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.UserPreferences);
        }
    }
}
