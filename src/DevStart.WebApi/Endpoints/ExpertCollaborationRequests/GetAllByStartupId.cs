using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ExpertCollaborationRequests.GetAllByStartupId;
using DevStart.Application.ExpertCollaborationRequests.GetById;
using DevStart.Domain.ExpertCollaborationRequests;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DevStart.WebApi.Endpoints.ExpertCollaborationRequests
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/startups/{startupId:guid}/expert-collaboration-requests", async (
                Guid startupId,
                [FromQuery] ExpertCollaborationRequestStatus? status,
                [FromQuery] int? pageNumber,
                [FromQuery] int? pageSize,
                IQueryHandler<GetExpertCollaborationRequestsByStartupIdQuery, List<ExpertCollaborationRequestResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                // Nullable so the paging parameters stay optional — a non-nullable int would make
                // minimal APIs reject a call that omits them. Zero falls back to the default page size.
                var query = new GetExpertCollaborationRequestsByStartupIdQuery(
                    startupId, status, pageNumber ?? 1, pageSize ?? 0);
                Result<List<ExpertCollaborationRequestResponse>> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.ExpertCollaborationRequestsRead)
                .WithTags(Tags.ExpertCollaborationRequests);
        }
    }
}
