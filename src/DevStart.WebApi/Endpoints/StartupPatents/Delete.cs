using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupPatents.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupPatents
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/startup-patents/{patentId:guid}", async (
                Guid patentId,
                ICommandHandler<DeleteStartupPatentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                Result result = await handler.Handle(new DeleteStartupPatentCommand(patentId), cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupPatentsDelete)
                .WithTags(Tags.StartupPatents);
        }
    }
}
