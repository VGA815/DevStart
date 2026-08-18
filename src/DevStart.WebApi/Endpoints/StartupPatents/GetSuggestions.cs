using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupPatents.GetSuggestions;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupPatents
{
    /// <summary>
    /// Records the register already attributes to the declared ИНН, minus the ones already listed —
    /// the reverse of the resolve query, and member-only because it is a filling aid.
    /// </summary>
    internal sealed class GetSuggestions : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/patents/suggestions", async (
                Guid startupId,
                IQueryHandler<GetStartupPatentSuggestionsQuery, StartupPatentSuggestionsResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupPatentSuggestionsQuery(startupId);
                Result<StartupPatentSuggestionsResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupPatentsRead)
                .WithTags(Tags.StartupPatents);
        }
    }
}
