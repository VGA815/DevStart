using DevStart.Application.Abstractions.Data;
using DevStart.Infrastructure.DealDocuments;
using System.Reflection;

namespace DevStart.UnitTests.TestSupport;

/// <summary>
/// Serves the term-sheet templates straight out of the Infrastructure assembly's embedded resources —
/// the same bytes <see cref="TemplatesSeeder"/> uploads to MinIO on boot. Rendering tests therefore
/// exercise the real templates rather than a copy that can drift away from them.
/// </summary>
internal sealed class TemplateFileStorage : IFileStorage
{
    public Task<Stream> DownloadAsync(string objectName, string bucket, CancellationToken cancellationToken)
    {
        Assembly assembly = typeof(TemplatesSeeder).Assembly;
        string resource = $"{assembly.GetName().Name}.DealDocuments.Templates.{objectName}";
        Stream? stream = assembly.GetManifestResourceStream(resource);
        return stream is null
            ? throw new InvalidOperationException($"Embedded template not found: {resource}")
            : Task.FromResult(stream);
    }

    public Task UploadAsync(string objectKey, Stream data, string bucket, string contentType, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task DeleteAsync(string objectKey, string bucket, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task<string> GetPresignedUrl(string objectKey, string bucket, int expirySeconds, CancellationToken cancellationToken, string? downloadFileName = null)
        => Task.FromResult($"https://example.com/{bucket}/{objectKey}");
}
