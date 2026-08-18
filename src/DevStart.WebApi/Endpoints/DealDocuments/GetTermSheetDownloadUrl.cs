using DevStart.Application.Abstractions.Authorization;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.DealDocuments.GetTermSheetDownloadUrl;
using DevStart.SharedKernel;
using DevStart.WebApi.Extensions;
using DevStart.WebApi.Infrastructure;

namespace DevStart.WebApi.Endpoints.DealDocuments
{
    internal sealed class GetTermSheetDownloadUrl : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("api/investment-deals/{dealId:guid}/term-sheet/download", async (
                Guid dealId,
                string? format,
                IQueryHandler<GetTermSheetDownloadUrlQuery, TermSheetDownloadUrlResponse> handler,
                CancellationToken cancellationToken) =>
            {
                // Defaults to markdown so links made before the PDF existed keep resolving to the
                // file they always did.
                TermSheetFormat requested = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
                    ? TermSheetFormat.Pdf
                    : TermSheetFormat.Markdown;

                var query = new GetTermSheetDownloadUrlQuery(dealId, requested);
                Result<TermSheetDownloadUrlResponse> result = await handler.Handle(query, cancellationToken);
                return result.Match(Results.Ok, CustomResults.Problem);
            })
                .HasPermission(Permissions.DealDocumentsRead)
                .WithTags(Tags.DealDocuments);
        }
    }
}
