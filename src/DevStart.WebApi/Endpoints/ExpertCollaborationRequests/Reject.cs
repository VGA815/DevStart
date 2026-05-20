using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.Reject;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ExpertCollaborationRequests
{
    internal sealed class Reject : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/expert-collaboration-requests/{requestId:guid}/reject", async (
                Guid requestId,
                ICommandHandler<RejectExpertCollaborationRequestCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RejectExpertCollaborationRequestCommand(requestId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsRespond)
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
