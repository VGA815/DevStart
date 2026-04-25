using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentDeals.ConfirmByInvestor;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentDeals
{
    internal sealed class ConfirmByInvestor : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investment-deals/{dealId:guid}/confirm-investor", async (
                Guid dealId,
                ICommandHandler<ConfirmInvestmentDealByInvestorCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new ConfirmInvestmentDealByInvestorCommand(dealId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentDealsConfirm)
                .WithTags(Tags.InvestmentDeals);
        }
    }
}
