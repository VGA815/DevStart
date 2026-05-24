using DevStart.Application.Abstractions.Data;

namespace DevStart.UnitTests.TestSupport;

internal sealed class CapturingFileStorage : IFileStorage
{
    public List<UploadCall> Uploads { get; } = [];

    public Task UploadAsync(
        string objectKey,
        Stream data,
        string bucket,
        string contentType,
        CancellationToken cancellationToken)
    {
        Uploads.Add(new UploadCall(objectKey, bucket, contentType, data.Length));

        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(
        string objectName,
        string bucket,
        CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(
        string objectKey,
        string bucket,
        CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<string> GetPresignedUrl(
        string objectKey,
        string bucket,
        int expirySeconds,
        CancellationToken cancellationToken) =>
        Task.FromResult($"https://example.com/{bucket}/{objectKey}?expires={expirySeconds}");

    internal sealed record UploadCall(string ObjectKey, string Bucket, string ContentType, long Size);
}
