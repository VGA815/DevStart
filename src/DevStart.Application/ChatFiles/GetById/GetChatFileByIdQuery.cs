using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.ChatFiles.GetById
{
    public sealed record GetChatFileByIdQuery(Guid ChatFileId, int Expires) : IQuery<ChatFileResponse>;
}
