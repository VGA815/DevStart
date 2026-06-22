using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.Admin.PromoCodes.DeactivatePromoCode;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.Admin.PromoCodes
{
    internal sealed class DeactivatePromoCode : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/admin/promo-codes/{id:guid}/deactivate", async (
                Guid id,
                ICommandHandler<DeactivatePromoCodeCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new DeactivatePromoCodeCommand(id);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.AdminPromoCodesManage)
                .WithTags(Tags.Admin);
        }
    }
}
