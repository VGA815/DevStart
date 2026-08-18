using DevStart.Application.Abstractions.Data;

namespace DevStart.UnitTests.TestSupport;

internal sealed class CapturingFileStorage : IFileStorage
{
    public List<UploadCall> Uploads { get; } = [];
    public List<DeleteCall> Deletes { get; } = [];

    // When set, the corresponding operation throws instead of succeeding — used to simulate a storage
    // outage (or a missing object) so handlers can be verified to translate it into a Result.
    public Exception? UploadException { get; set; }
    public Exception? DownloadException { get; set; }
    public Exception? DeleteException { get; set; }
    public Exception? PresignException { get; set; }

    public Task UploadAsync(
        string objectKey,
        Stream data,
        string bucket,
        string contentType,
        CancellationToken cancellationToken)
    {
        if (UploadException is not null)
        {
            throw UploadException;
        }

        Uploads.Add(new UploadCall(objectKey, bucket, contentType, data.Length));

        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(
        string objectName,
        string bucket,
        CancellationToken cancellationToken) =>
        DownloadException is not null
            ? throw DownloadException
            : Task.FromResult<Stream>(new MemoryStream());

    public Task DeleteAsync(
        string objectKey,
        string bucket,
        CancellationToken cancellationToken)
    {
        if (DeleteException is not null)
        {
            throw DeleteException;
        }

        Deletes.Add(new DeleteCall(objectKey, bucket));

        return Task.CompletedTask;
    }

    public List<PresignCall> Presigns { get; } = [];

    public Task<string> GetPresignedUrl(
        string objectKey,
        string bucket,
        int expirySeconds,
        CancellationToken cancellationToken,
        string? downloadFileName = null)
    {
        if (PresignException is not null)
        {
            throw PresignException;
        }

        Presigns.Add(new PresignCall(objectKey, bucket, expirySeconds, downloadFileName));

        return Task.FromResult($"https://example.com/{bucket}/{objectKey}?expires={expirySeconds}");
    }

    internal sealed record UploadCall(string ObjectKey, string Bucket, string ContentType, long Size);

    internal sealed record DeleteCall(string ObjectKey, string Bucket);

    internal sealed record PresignCall(string ObjectKey, string Bucket, int ExpirySeconds, string? DownloadFileName);
}
