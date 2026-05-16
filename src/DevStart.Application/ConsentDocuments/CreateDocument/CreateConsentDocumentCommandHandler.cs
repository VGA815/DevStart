using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ConsentDocuments;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.ConsentDocuments.CreateDocument
{
    internal sealed class CreateConsentDocumentCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateConsentDocumentCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(
            CreateConsentDocumentCommand command,
            CancellationToken cancellationToken)
        {
            bool versionExists = await context.ConsentDocuments
                .AnyAsync(d => d.Type == command.Type && d.Version == command.Version, cancellationToken);

            if (versionExists)
            {
                return Result.Failure<Guid>(
                    ConsentDocumentErrors.VersionAlreadyExists(command.Type, command.Version));
            }

            ConsentDocument document = ConsentDocument.Create(
                command.Type,
                command.Version,
                command.Title,
                command.Content,
                dateTimeProvider.UtcNow);

            context.ConsentDocuments.Add(document);
            await context.SaveChangesAsync(cancellationToken);

            return document.Id;
        }
    }
}
