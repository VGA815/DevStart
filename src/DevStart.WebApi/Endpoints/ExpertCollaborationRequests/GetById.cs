using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ExpertCollaborationRequests
{
    internal sealed class GetById : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/expert-collaboration-requests/{requestId:guid}", async (
                Guid requestId,
                IQueryHandler<GetExpertCollaborationRequestByIdQuery, ExpertCollaborationRequestResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetExpertCollaborationRequestByIdQuery(requestId);
                Result<ExpertCollaborationRequestResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsRead)
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
