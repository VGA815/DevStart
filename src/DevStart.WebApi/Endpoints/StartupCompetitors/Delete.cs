using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupCompetitors.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupCompetitors
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/startup-competitors/{competitorId:guid}", async (
                Guid competitorId,
                ICommandHandler<DeleteStartupCompetitorCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteStartupCompetitorCommand(competitorId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupCompetitorsDelete)
                .WithTags(Tags.StartupCompetitors);
        }
    }
}
