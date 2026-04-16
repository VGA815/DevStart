using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InviteTokens.ValidateToken;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InviteTokens
{
    internal sealed class ValidateToken : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/invite-tokens/{tokenId:guid}", async (
                IQueryHandler<ValidateTokenQuery, bool> handler,
                Guid tokenId,
                CancellationToken cancellationToken) =>
            {
                ValidateTokenQuery query = new(tokenId);

                Result<bool> result = await handler.Handle(query, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .RequireAuthorization()
                .WithTags(Tags.InviteTokens);
        }
    }
}
