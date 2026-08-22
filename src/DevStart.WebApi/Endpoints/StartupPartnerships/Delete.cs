using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.StartupPartnerships.Delete;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.StartupPartnerships
{
    internal sealed class Delete : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapDelete("api/startup-partnerships/{partnershipId:guid}", async (
                Guid partnershipId,
                ICommandHandler<DeleteStartupPartnershipCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteStartupPartnershipCommand(partnershipId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.StartupPartnershipsDelete)
                .WithTags(Tags.StartupPartnerships);
        }
    }
}
