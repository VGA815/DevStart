using DevStart.Application.Abstractions.Messaging;

namespace DevStart.Application.MediaFiles.GetById
{
    public sealed record GetMediaFileByIdQuery(Guid FileId, int Expires) : IQuery<MediaFileResponse>;
}