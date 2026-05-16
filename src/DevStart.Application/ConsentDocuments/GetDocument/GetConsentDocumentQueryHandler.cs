using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Application.ConsentDocuments.GetDocuments;
using DevStart.Domain.ConsentDocuments;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ConsentDocuments.GetDocument
{
    internal sealed class GetConsentDocumentQueryHandler(IApplicationDbContext context)
        : IQueryHandler<GetConsentDocumentQuery, ConsentDocumentResponse>
    {
        public async Task<Result<ConsentDocumentResponse>> Handle(
            GetConsentDocumentQuery query,
            CancellationToken cancellationToken)
        {
            ConsentDocumentResponse? document = await context.ConsentDocuments
                .Where(d => d.Type == query.Type &&
                            (query.Version == null ? d.IsActive : d.Version == query.Version))
                .Select(d => new ConsentDocumentResponse
                {
                    Id        = d.Id,
                    Type      = d.Type,
                    Version   = d.Version,
                    Title     = d.Title,
                    Content   = d.Content,
                    CreatedAt = d.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (document is null)
            {
                return query.Version is null
                    ? Result.Failure<ConsentDocumentResponse>(ConsentDocumentErrors.NoActiveDocument(query.Type))
                    : Result.Failure<ConsentDocumentResponse>(
                        ConsentDocumentErrors.NotFound(Guid.Empty));
            }

            return document;
        }
    }
}
