using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentApplications.Accept;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentApplications
{
    internal sealed class Accept : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investment-applications/{applicationId:guid}/accept", async (
                Guid applicationId,
                ICommandHandler<AcceptInvestmentApplicationCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new AcceptInvestmentApplicationCommand(applicationId);
                Result<Guid> result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentApplicationsRespond)
                .WithTags(Tags.InvestmentApplications);
        }
    }
}
