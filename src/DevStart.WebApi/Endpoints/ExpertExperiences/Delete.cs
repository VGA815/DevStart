using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertExperiences.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.ExpertExperiences
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/expert-experiences", async (
                [FromQuery] Guid id,
                ICommandHandler<DeleteExpertExperienceCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteExpertExperienceCommand(id);

                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertExperiencesDelete)
                .WithTags(Tags.ExpertExperiences);
        }
    }
}
