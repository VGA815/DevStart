using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.DealDocuments.Generation;
using DevStart.Application.DealDocuments.GetCapTable;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.DealDocuments
{
    internal sealed class GetCapTable : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/investment-deals/{dealId:guid}/cap-table", async (
                Guid dealId,
                IQueryHandler<GetCapTableQuery, CapTableResult> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetCapTableQuery(dealId);
                Result<CapTableResult> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.DealDocumentsRead)
                .WithTags(Tags.DealDocuments);
        }
    }
}
