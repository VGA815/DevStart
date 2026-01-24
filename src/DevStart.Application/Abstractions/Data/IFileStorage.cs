namespace DevStart.Application.Abstractions.Data
{
    public interface IFileStorage
    {
        Task UploadAsync(
            string objectKey,
            Stream data,
            string contentType,
            CancellationToken cancellationToken);

        Task<Stream> DownloadAsync(
            string objectName,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            string objectKey,
            CancellationToken cancellationToken);
    }
}
