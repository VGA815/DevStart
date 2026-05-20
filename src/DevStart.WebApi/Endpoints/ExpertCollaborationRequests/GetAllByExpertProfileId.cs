using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetAllByExpertProfileId;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.ExpertCollaborationRequests
{
    internal sealed class GetAllByExpertProfileId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/expert-profiles/{expertProfileId:guid}/expert-collaboration-requests", async (
                Guid expertProfileId,
                IQueryHandler<GetExpertCollaborationRequestsByExpertProfileIdQuery, List<ExpertCollaborationRequestResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetExpertCollaborationRequestsByExpertProfileIdQuery(expertProfileId);
                Result<List<ExpertCollaborationRequestResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsRead)
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
