using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.Withdraw;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ExpertCollaborationRequests
{
    internal sealed class Withdraw : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/expert-collaboration-requests/{requestId:guid}/withdraw", async (
                Guid requestId,
                ICommandHandler<WithdrawExpertCollaborationRequestCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new WithdrawExpertCollaborationRequestCommand(requestId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsWithdraw)
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
