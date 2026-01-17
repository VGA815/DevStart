
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Startups.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.Startups
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("startups", async (
                [FromQuery] Guid startupId, 
                ICommandHandler<DeleteStartupCommand> handler, 
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteStartupCommand(startupId);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.Startups);
        }
    }
}
