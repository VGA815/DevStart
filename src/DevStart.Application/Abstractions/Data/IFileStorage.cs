namespace DevStart.Application.Abstractions.Data
{
    public interface IFileStorage
    {
        Task UploadAsync(
            string objectKey,
            Stream data,
            string bucket,
            string contentType,
            CancellationToken cancellationToken);

        Task<Stream> DownloadAsync(
            string objectName,
            string bucket,
            CancellationToken cancellationToken);

        Task DeleteAsync(
            string objectKey,
            string bucket,
            CancellationToken cancellationToken);
    }
}
