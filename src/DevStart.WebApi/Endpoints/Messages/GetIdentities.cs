using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Messages.GetIdentities;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Messages
{
    internal sealed class GetIdentities : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/messages/identities", async (
                IQueryHandler<GetChatIdentitiesQuery, List<ChatIdentityResponse>> handler,
                CancellationToken cancellationToken) =>
            {
                Result<List<ChatIdentityResponse>> result = await handler.Handle(new GetChatIdentitiesQuery(), cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.MessagesRead)
                .WithTags(Tags.Messages);
        }
    }
}
