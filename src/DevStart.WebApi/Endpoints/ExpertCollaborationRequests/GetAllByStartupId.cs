using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetAllByStartupId;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ExpertCollaborationRequests
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/expert-collaboration-requests", async (
                Guid startupId,
                IQueryHandler<GetExpertCollaborationRequestsByStartupIdQuery, List<ExpertCollaborationRequestResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetExpertCollaborationRequestsByStartupIdQuery(startupId);
                Result<List<ExpertCollaborationRequestResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsRead)
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
