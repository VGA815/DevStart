using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ConsentDocuments.GetDocuments
{
    internal sealed class GetConsentDocumentsQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetConsentDocumentsQuery, List<ConsentDocumentResponse>>
    {
        public async Task<Result<List<ConsentDocumentResponse>>> Handle(
            GetConsentDocumentsQuery query,
            CancellationToken cancellationToken)
        {
            List<ConsentDocumentResponse> documents = await context.ConsentDocuments
                .Where(d => d.IsActive)
                .OrderBy(d => d.Type)
                .Select(d => new ConsentDocumentResponse
                {
                    Id        = d.Id,
                    Type      = d.Type,
                    Version   = d.Version,
                    Title     = d.Title,
                    Content   = d.Content,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return documents;
        }
    }
}
