using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.InvestmentDeals.RegenerateDocuments;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.InvestmentDeals
{
    internal sealed class RegenerateDocuments : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/investment-deals/{dealId:guid}/regenerate-documents", async (
                Guid dealId,
                ICommandHandler<RegenerateDealDocumentsCommand> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RegenerateDealDocumentsCommand(dealId);
                Result result = await handler.Handle(command, cancellationToken);
                return result.Match(Results.NoContent, CustomResults.Problem);
            })
                .HasPermission(Permissions.InvestmentDealsRead)
                .WithTags(Tags.InvestmentDeals);
        }
    }
}
