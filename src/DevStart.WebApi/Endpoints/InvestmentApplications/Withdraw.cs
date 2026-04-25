using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.Withdraw;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class Withdraw : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investment-applications/{applicationId:guid}/withdraw", async (
                Guid applicationId,
                ICommandHandler<WithdrawInvestmentApplicationCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new WithdrawInvestmentApplicationCommand(applicationId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsWithdraw)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
