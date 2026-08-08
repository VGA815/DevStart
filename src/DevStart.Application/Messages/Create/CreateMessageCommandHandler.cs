using DevStart.Application.Abstractions.Authentication;
using DevStart.Application.Abstractions.Data;
using DevStart.Application.Abstractions.Messaging;
using DevStart.Domain.ChatFiles;
using DevStart.Domain.Messages;
using DevStart.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace DevStart.Application.Messages.Create
{
    internal sealed class CreateMessageCommandHandler(IApplicationDbContext context, IUserContext userContext, IDateTimeProvider dateTimeProvider)
        : ICommandHandler<CreateMessageCommand, Guid>
    {
        public async Task<Result<Guid>> Handle(CreateMessageCommand command, CancellationToken cancellationToken)
        {
            string? textContent = string.IsNullOrWhiteSpace(command.TextContent) ? null : command.TextContent.Trim();

            List<Guid> mediaIds = Normalize(command.MediaIds);
            List<Guid> metricIds = Normalize(command.MetricIds);
            List<Guid> documentIds = Normalize(command.DocumentIds);
            List<Guid> fileIds = Normalize(command.FileIds);

            if (textContent is null
                && metricIds.Count == 0
                && mediaIds.Count == 0
                && documentIds.Count == 0
                && fileIds.Count == 0)
            {
                return Result.Failure<Guid>(MessageErrors.IsEmpty);
            }

            Guid userId = userContext.UserId;
            Guid senderId;
            ChatParticipantType senderType;
            Guid? sentByProfileId = null;

            if (command.SenderStartupId.HasValue)
            {
                if (!await StartupIdentity.CanActAsAsync(context, command.SenderStartupId.Value, userId, cancellationToken))
                {
                    return Result.Failure<Guid>(MessageErrors.StartupIdentityForbidden);
                }

                senderId = command.SenderStartupId.Value;
                senderType = ChatParticipantType.Startup;
                // Kept for the startup's own side of the thread, so the team can tell who replied.
                sentByProfileId = userId;
            }
            else
            {
                senderId = userId;
                senderType = ChatParticipantType.User;
            }

            if (senderType == command.ReceiverType && senderId == command.ReceiverId)
            {
                return Result.Failure<Guid>(MessageErrors.Unauthorized);
            }

            bool receiverExists = command.ReceiverType switch
            {
                ChatParticipantType.User => await context.Users.AnyAsync(u => u.Id == command.ReceiverId, cancellationToken),
                ChatParticipantType.Startup => await context.Startups.AnyAsync(s => s.Id == command.ReceiverId, cancellationToken),
                _ => false
            };

            if (!receiverExists)
            {
                return Result.Failure<Guid>(MessageErrors.ReceiverNotFound(command.ReceiverId, command.ReceiverType));
            }

            // Attachments are referenced by raw id, so every one of them has to be proven to belong to the
            // sender — otherwise anyone could attach (and thereby read) an arbitrary metric or document.
            if (mediaIds.Count > 0)
            {
                int owned = await context.MediaFiles
                    .CountAsync(f => mediaIds.Contains(f.Id) && f.UploaderId == userId, cancellationToken);

                if (owned != mediaIds.Count)
                {
                    return Result.Failure<Guid>(MessageErrors.AttachmentNotAllowed("images"));
                }
            }

            if (metricIds.Count > 0 || documentIds.Count > 0)
            {
                List<Guid> myStartupIds = await context.StartupMembers
                    .Where(sm => sm.ProfileId == userId)
                    .Select(sm => sm.StartupId)
                    .ToListAsync(cancellationToken);

                if (metricIds.Count > 0)
                {
                    int allowed = await context.StartupMetrics
                        .CountAsync(m => metricIds.Contains(m.Id) && myStartupIds.Contains(m.StartupId), cancellationToken);

                    if (allowed != metricIds.Count)
                    {
                        return Result.Failure<Guid>(MessageErrors.AttachmentNotAllowed("metrics"));
                    }
                }

                if (documentIds.Count > 0)
                {
                    int allowed = await context.StartupDocumentFiles
                        .CountAsync(d => documentIds.Contains(d.Id) && myStartupIds.Contains(d.StartupId), cancellationToken);

                    if (allowed != documentIds.Count)
                    {
                        return Result.Failure<Guid>(MessageErrors.AttachmentNotAllowed("documents"));
                    }
                }
            }

            List<ChatFile> chatFiles = [];
            if (fileIds.Count > 0)
            {
                chatFiles = await context.ChatFiles
                    .Where(f => fileIds.Contains(f.Id) && f.UploaderId == userId && f.MessageId == null)
                    .ToListAsync(cancellationToken);

                if (chatFiles.Count != fileIds.Count)
                {
                    return Result.Failure<Guid>(MessageErrors.AttachmentNotAllowed("files"));
                }
            }

            Message message = Message.Create(
                senderId,
                senderType,
                sentByProfileId,
                command.ReceiverId,
                command.ReceiverType,
                textContent,
                mediaIds,
                metricIds,
                documentIds,
                fileIds,
                dateTimeProvider.UtcNow);

            foreach (ChatFile chatFile in chatFiles)
            {
                // Binds read access for the file to the message it travelled with.
                chatFile.AttachTo(message.Id);
            }

            message.Raise(new MessageCreatedDomainEvent(
                message.Id,
                message.SenderId,
                message.SenderType,
                message.SentByProfileId,
                message.ReceiverId,
                message.ReceiverType));

            context.Messages.Add(message);
            await context.SaveChangesAsync(cancellationToken);

            return message.Id;
        }

        private static List<Guid> Normalize(List<Guid>? ids) =>
            ids is null ? [] : [.. ids.Where(id => id != Guid.Empty).Distinct()];
    }
}
