using DevStart.Application.Abstractions.Data;
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
        public MinioFileStorage(IOptions<MinioOptions> options)
        {
            var o = options.Value;
            
            _internalMinioClient = (MinioClient)new MinioClient()
                .WithEndpoint(o.Endpoint)
                .WithCredentials(o.AccessKey, o.SecretKey)
                .WithSSL(o.UseSsl)
                .Build();

            _externalMinioClient = (MinioClient)new MinioClient()
                .WithEndpoint(o.PubEndpoint)
                .WithCredentials(o.AccessKey, o.SecretKey)
                .WithSSL(o.UseSsl)
                .Build();
        }
        public async Task UploadAsync(
            string objectName,
            Stream data,
            string bucket,
            string contentType,
            CancellationToken ct)
        {
            await EnsureBucketExists(bucket, ct);

            var args = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(data)
                .WithObjectSize(data.Length)
                .WithContentType(contentType);

            await _internalMinioClient.PutObjectAsync(args, ct);
        }

        public async Task<Stream> DownloadAsync(
            string objectName,
            string bucket,
            CancellationToken ct)
        {
            var ms = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithCallbackStream(s => s.CopyTo(ms));

            await _internalMinioClient.GetObjectAsync(args, ct);

            ms.Position = 0;
            return ms;
        }

        public async Task DeleteAsync(
            string objectName,
            string bucket,
            CancellationToken ct)
        {
            var args = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName);

            await _internalMinioClient.RemoveObjectAsync(args, ct);
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
            return await _externalMinioClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(bucket)
                    .WithExpiry(expirySeconds)
                    .WithObject(objectKey));
        }
    }
}
