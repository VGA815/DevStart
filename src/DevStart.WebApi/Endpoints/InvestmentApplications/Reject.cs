using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.Reject;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class Reject : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investment-applications/{applicationId:guid}/reject", async (
                Guid applicationId,
                ICommandHandler<RejectInvestmentApplicationCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RejectInvestmentApplicationCommand(applicationId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsRespond)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
