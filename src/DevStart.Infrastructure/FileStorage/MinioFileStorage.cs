using DevStart.Application.Abstractions.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using System.Text;

namespace DevStart.Infrastructure.FileStorage
{
    public class MinioFileStorage : IFileStorage
    {
        private readonly IMinioClient _internalMinioClient;
        private readonly IMinioClient _externalMinioClient;
        private readonly ILogger<MinioFileStorage> _logger;
        public MinioFileStorage(IMinioClient internalClient, IOptions<MinioOptions> options, ILogger<MinioFileStorage> logger)
        {
            var o = options.Value;
            _logger = logger;

            // Internal-endpoint client comes from DI (shared with the health check); the presign
            // client targets the public endpoint, so it is built here.
            _internalMinioClient = internalClient;

            _externalMinioClient = new MinioClient()
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

        public async Task<string> GetPresignedUrl(
            string objectKey,
            string bucket,
            int expirySeconds,
            CancellationToken cancellationToken,
            string? downloadFileName = null)
        {
            var args = new PresignedGetObjectArgs()
                .WithBucket(bucket)
                .WithExpiry(expirySeconds)
                .WithObject(objectKey);

            if (!string.IsNullOrEmpty(downloadFileName))
            {
                // The SDK emits this as a signed `response-content-disposition` query parameter,
                // not as a request header, so the name is part of what the signature covers and
                // cannot be swapped by whoever holds the link. Pinned by a test, because it is an
                // SDK behaviour a version bump could change without any error surfacing here.
                args = args.WithHeaders(new Dictionary<string, string>
                {
                    ["response-content-disposition"] =
                        $"attachment; filename=\"{SanitizeFileName(downloadFileName)}\""
                });
            }

            return await GuardAsync(() => _externalMinioClient.PresignedGetObjectAsync(args),
                "presign", bucket, objectKey, cancellationToken);
        }

        /// <summary>
        /// Reduces a download name to characters that are safe inside a quoted
        /// Content-Disposition value.
        /// <para>
        /// Today every caller passes a name this class built itself, so nothing unsafe reaches
        /// here. But the parameter sits on a public storage abstraction, and the value ends up
        /// inside a response header the object store emits: a quote would end the filename early,
        /// and CR/LF could split the header. Stripping is done here rather than trusted upstream,
        /// because the caller that eventually forgets will not be this one.
        /// </para>
        /// </summary>
        private static string SanitizeFileName(string fileName)
        {
            var safe = new StringBuilder(fileName.Length);
            foreach (char c in fileName)
            {
                bool allowed = c is >= (char)0x20 and < (char)0x7f
                    && c is not ('"' or '\\' or ';');
                safe.Append(allowed ? c : '_');
            }

            return safe.ToString();
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
