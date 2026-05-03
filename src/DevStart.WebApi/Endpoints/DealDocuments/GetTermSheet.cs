using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.DealDocuments.GetTermSheet;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.DealDocuments
{
    internal sealed class GetTermSheet : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/investment-deals/{dealId:guid}/term-sheet", async (
                Guid dealId,
                IQueryHandler<GetTermSheetQuery, TermSheetResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetTermSheetQuery(dealId);
                Result<TermSheetResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.DealDocumentsRead)
                .WithTags(Tags.DealDocuments);
        }
    }
}
