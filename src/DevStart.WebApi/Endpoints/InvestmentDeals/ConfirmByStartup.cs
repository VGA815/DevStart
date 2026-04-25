using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentDeals.ConfirmByStartup;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentDeals
{
    internal sealed class ConfirmByStartup : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investment-deals/{dealId:guid}/confirm-startup", async (
                Guid dealId,
                ICommandHandler<ConfirmInvestmentDealByStartupCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ConfirmInvestmentDealByStartupCommand(dealId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentDealsConfirm)
                .WithTags(Tags.InvestmentDeals);
        }
    }
}
