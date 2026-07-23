using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.CommunityStandards;
using DevStart.Application.CommunityStandards.GetTemplates;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.CommunityStandards
{
    internal sealed class GetTemplates : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/community-standards/templates", async (
                IQueryHandler<GetCommunityDocumentTemplatesQuery, List<CommunityDocumentTemplate>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<List<CommunityDocumentTemplate>> result =
                    await handler.Handle(new GetCommunityDocumentTemplatesQuery(), cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.CommunityDocumentsManage)
                .WithTags(Tags.CommunityStandards);
        }
    }
}
