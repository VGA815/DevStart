using System.Collections.Concurrent;
using DevStart.Application.Abstractions.Data;

namespace DevStart.IntegrationTests.Fakes
{
    /// <summary>In-memory <see cref="IFileStorage"/> replacing MinIO. Keeps uploaded blobs in a dictionary
    /// keyed by "bucket/objectKey" so the startup template seeder and any upload flows succeed offline.</summary>
    internal sealed class FakeFileStorage : IFileStorage
    {
        private readonly ConcurrentDictionary<string, byte[]> _objects = new();

        private static string Key(string bucket, string objectKey) => $"{bucket}/{objectKey}";

        public async Task UploadAsync(string objectKey, Stream data, string bucket, string contentType, CancellationToken cancellationToken)
        {
            using var ms = new MemoryStream();
            await data.CopyToAsync(ms, cancellationToken);
            _objects[Key(bucket, objectKey)] = ms.ToArray();
        }

        public Task<Stream> DownloadAsync(string objectName, string bucket, CancellationToken cancellationToken)
        {
            if (_objects.TryGetValue(Key(bucket, objectName), out byte[]? bytes))
            {
                return Task.FromResult<Stream>(new MemoryStream(bytes));
            }

            throw new FileStorageException($"Object '{objectName}' not found in bucket '{bucket}'.", notFound: true);
        }

        public Task DeleteAsync(string objectKey, string bucket, CancellationToken cancellationToken)
        {
            _objects.TryRemove(Key(bucket, objectKey), out _);
            return Task.CompletedTask;
        }

        public Task<string> GetPresignedUrl(string objectKey, string bucket, int expirySeconds, CancellationToken cancellationToken)
            => Task.FromResult($"https://files.test.local/{bucket}/{objectKey}?expires={expirySeconds}");
    }
}
