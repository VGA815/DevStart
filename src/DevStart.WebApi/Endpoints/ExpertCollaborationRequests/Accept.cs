using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.Accept;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ExpertCollaborationRequests
{
    internal sealed class Accept : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/expert-collaboration-requests/{requestId:guid}/accept", async (
                Guid requestId,
                ICommandHandler<AcceptExpertCollaborationRequestCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AcceptExpertCollaborationRequestCommand(requestId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsRespond)
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
