using DevStart.Application.Abstractions.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace DevStart.Infrastructure.FileStorage
{
    public class MinioFileStorage : IFileStorage
    {
        private readonly MinioClient _internalMinioClient;
        private readonly MinioClient _externalMinioClient;
        private readonly ILogger<MinioFileStorage> _logger;
        public MinioFileStorage(IOptions<MinioOptions> options, ILogger<MinioFileStorage> logger)
        {
            var o = options.Value;
            _logger = logger;

            _internalMinioClient = (MinioClient)new MinioClient()
                .WithEndpoint(o.Endpoint)
                .WithCredentials(o.AccessKey, o.SecretKey)
                .WithSSL(o.UseSsl)
                .Build();

            _externalMinioClient = (MinioClient)new MinioClient()
                .WithEndpoint(o.PubEndpoint)
                .WithCredentials(o.AccessKey, o.SecretKey)
                .WithSSL(o.PubUseSsl)
                .Build();
        }
        public async Task UploadAsync(
            string objectName,
            Stream data,
            string bucket,
            string contentType,
            CancellationToken ct)
        {
            await GuardAsync(async () =>
            {
                await EnsureBucketExists(bucket, ct);

                var args = new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName)
                    .WithStreamData(data)
                    .WithObjectSize(data.Length)
                    .WithContentType(contentType);

                await _internalMinioClient.PutObjectAsync(args, ct);
                return true;
            }, "upload", bucket, objectName, ct);
        }

        public async Task<Stream> DownloadAsync(
            string objectName,
            string bucket,
            CancellationToken ct)
        {
            return await GuardAsync<Stream>(async () =>
            {
                var ms = new MemoryStream();

                var args = new GetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName)
                    .WithCallbackStream(s => s.CopyTo(ms));

                await _internalMinioClient.GetObjectAsync(args, ct);

                ms.Position = 0;
                return ms;
            }, "download", bucket, objectName, ct);
        }

        public async Task DeleteAsync(
            string objectName,
            string bucket,
            CancellationToken ct)
        {
            await GuardAsync(async () =>
            {
                var args = new RemoveObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectName);

                await _internalMinioClient.RemoveObjectAsync(args, ct);
                return true;
            }, "delete", bucket, objectName, ct);
        }

        private async Task EnsureBucketExists(string bucket, CancellationToken ct)
        {
            var exists = await _internalMinioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucket), ct);

            if (!exists)
            {
                await _internalMinioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(bucket), ct);
            }
        }

        public async Task<string> GetPresignedUrl(string objectKey, string bucket, int expirySeconds, CancellationToken cancellationToken)
        {
            return await GuardAsync(() => _externalMinioClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(bucket)
                    .WithExpiry(expirySeconds)
                    .WithObject(objectKey)), "presign", bucket, objectKey, cancellationToken);
        }

        // Translate MinIO SDK / transport exceptions into a typed FileStorageException so callers can
        // return a clean 404 (object missing) or 503 (storage outage) instead of leaking a raw SDK
        // exception as an unhandled 500. Genuine caller cancellation is preserved.
        private async Task<T> GuardAsync<T>(
            Func<Task<T>> operation,
            string op,
            string bucket,
            string objectName,
            CancellationToken ct)
        {
            try
            {
                return await operation();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                bool notFound = ex.GetType().Name.Contains("NotFound", StringComparison.OrdinalIgnoreCase);
                _logger.Log(
                    notFound ? LogLevel.Warning : LogLevel.Error,
                    ex,
                    "MinIO {Operation} failed for {Bucket}/{ObjectName}",
                    op, bucket, objectName);
                throw new FileStorageException($"Object storage operation '{op}' failed.", notFound, ex);
            }
        }
    }
}
