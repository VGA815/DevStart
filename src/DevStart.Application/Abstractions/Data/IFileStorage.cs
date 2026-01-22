namespace DevStart.Application.Abstractions.Data
{
    public interface IFileStorage
    {
        Task UploadAsync(
            string objectKey,
            Stream data,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<string> GetPresignedUrlAsync(
            string objectKey,
            TimeSpan expiresIn,
            CancellationToken cancellationToken= default);

        Task DeleteAsync(
            string objectKey,
            CancellationToken cancellationToken = default);
    }
}
