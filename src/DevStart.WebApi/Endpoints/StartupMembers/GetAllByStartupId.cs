
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupMembers.GetAllByStartupId;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupMembers
{
    internal sealed class GetAllByStartupId : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("startups/{startupId:guid}/members", async (
                Guid startupId, 
                IQueryHandler<GetStartupMembersByStartupIdQuery, List<StartupMemberResponse>> handler, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetStartupMembersByStartupIdQuery(startupId);

                Result<List<StartupMemberResponse>> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .WithTags(Tags.StartupMembers);
        }
    }
}
