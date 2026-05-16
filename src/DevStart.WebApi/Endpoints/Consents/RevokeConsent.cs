using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.UserConsents.RevokeConsent;
using DevStart.Domain.UserConsents;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Consents
{
    internal sealed class RevokeConsent : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/consents/{type:int}", async (
                int type,
                ICommandHandler<RevokeConsentCommand> handler,
                CancellationToken cancellationToken) =>
            {
                if (!Enum.IsDefined(typeof(ConsentType), type))
                {
                    return Results.BadRequest($"Invalid consent type: {type}");
                }

                var command = new RevokeConsentCommand((ConsentType)type);

                Result result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.NoContent, CustomResults.Problem);
            })
            .HasPermission(Permissions.ConsentsRevoke)
            .WithTags(Tags.Consents);
        }
    }
}
