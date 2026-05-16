using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ConsentDocuments;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ConsentDocuments.ActivateDocument
{
    internal sealed class ActivateConsentDocumentCommandHandler(IApplicationDbContext context)
        : ICommandHandler<ActivateConsentDocumentCommand>
    {
        public async Task<Result> Handle(
            ActivateConsentDocumentCommand command,
            CancellationToken cancellationToken)
        {
            ConsentDocument? documentToActivate = await context.ConsentDocuments
                .FirstOrDefaultAsync(d => d.Id == command.DocumentId, cancellationToken);

            if (documentToActivate is null)
            {
                return Result.Failure(ConsentDocumentErrors.NotFound(command.DocumentId));
            }

            if (documentToActivate.IsActive)
            {
                return Result.Success();
            }

            // Deactivate the currently active document of the same type
            ConsentDocument? currentlyActive = await context.ConsentDocuments
                .FirstOrDefaultAsync(
                    d => d.Type == documentToActivate.Type && d.IsActive,
                    cancellationToken);

            currentlyActive?.Deactivate();

            documentToActivate.Activate();

            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
