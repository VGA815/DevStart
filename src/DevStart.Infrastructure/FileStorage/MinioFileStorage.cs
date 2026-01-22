using DevStart.Application.Abstractions.Data;
using Minio;
using Minio.DataModel.Args;

namespace DevStart.Infrastructure.FileStorage
{
    public class MinioFileStorage(MinioClient minioClient, string bucket) : IFileStorage
    {
        public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            await minioClient.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey),
                    cancellationToken);
        }

        public async Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiresIn, CancellationToken cancellationToken = default)
        {
            return await minioClient.PresignedGetObjectAsync(
                new PresignedGetObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey)
                    .WithExpiry((int)expiresIn.TotalSeconds));
        }

        public async Task UploadAsync(string objectKey, Stream data, string contentType, CancellationToken cancellationToken = default)
        {
            await minioClient.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(bucket)
                    .WithObject(objectKey)
                    .WithStreamData(data)
                    .WithObjectSize(data.Length)
                    .WithContentType(contentType),
                    cancellationToken);
        }
    }
}
