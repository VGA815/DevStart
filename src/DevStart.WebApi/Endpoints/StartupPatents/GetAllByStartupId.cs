using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupPatents.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupPatents
{
    /// <summary>
    /// The IP block of the Product tab: the claimed records, how each one stands against the local
    /// register copy, and the declared ИНН with whatever ЕГРЮЛ says about it. Non-matches are part of
    /// the answer — hiding them would make "enter twenty numbers, show the three that stick" a working
    /// tactic (SC-64).
    /// </summary>
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/patents", async (
                Guid startupId,
                IQueryHandler<GetStartupPatentsByStartupIdQuery, StartupPatentsResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupPatentsByStartupIdQuery(startupId);
                Result<StartupPatentsResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupPatentsRead)
                .WithTags(Tags.StartupPatents);
        }
    }
}
