namespace DevStart.Infrastructure.FileStorage
{
    public sealed class MinioOptions
    {
        public string Endpoint { get; init; } = null!;
        public string AccessKey { get; init; } = null!;
        public string SecretKey { get; init; } = null!;
        public string Bucket { get; init; } = null!;
        public bool UseSsl { get; init; }
        public string PubEndpoint { get; init; } = null!;
    }
}
