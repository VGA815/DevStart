using DevStart.Application.Abstractions.Data;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

namespace DevStart.Infrastructure.FileStorage
{
    public class MinioFileStorage : IFileStorage
    {
        private readonly MinioClient minioClient;
        private readonly string bucket;
        public MinioFileStorage(IOptions<MinioOptions> options)
        {
            var o = options.Value;
            
            bucket = o.Bucket;
            minioClient = (MinioClient)new MinioClient()
                .WithEndpoint(o.Endpoint)
                .WithCredentials(o.AccessKey, o.SecretKey)
                .WithSSL(o.UseSsl)
                .Build();
        }
        public async Task UploadAsync(
            string objectName,
            Stream data,
            string contentType,
            CancellationToken ct)
        {
            await EnsureBucketExists(ct);

            var args = new PutObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithStreamData(data)
                .WithObjectSize(data.Length)
                .WithContentType(contentType);

            await minioClient.PutObjectAsync(args, ct);
        }

        public async Task<Stream> DownloadAsync(
            string objectName,
            CancellationToken ct)
        {
            var ms = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName)
                .WithCallbackStream(s => s.CopyTo(ms));

            await minioClient.GetObjectAsync(args, ct);

            ms.Position = 0;
            return ms;
        }

        public async Task DeleteAsync(
            string objectName,
            CancellationToken ct)
        {
            var args = new RemoveObjectArgs()
                .WithBucket(bucket)
                .WithObject(objectName);

            await minioClient.RemoveObjectAsync(args, ct);
        }

        private async Task EnsureBucketExists(CancellationToken ct)
        {
            var exists = await minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(bucket), ct);

            if (!exists)
            {
                await minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(bucket), ct);
            }
        }
    }
}
