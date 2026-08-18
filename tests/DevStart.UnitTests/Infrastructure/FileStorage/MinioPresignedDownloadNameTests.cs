using DevStart.Infrastructure.FileStorage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using Shouldly;

namespace DevStart.UnitTests.Infrastructure.FileStorage;

/// <summary>
/// Pins how the download file name reaches the object store.
/// <para>
/// It is passed to the MinIO SDK as a "header", but for a presigned GET the SDK has to turn it into a
/// signed <c>response-content-disposition</c> query parameter — the S3 response-header override. If a
/// version bump ever made it sign a request header instead, nothing would throw: links would keep
/// working and every downloaded term sheet would quietly be called <c>term-sheet.pdf</c> again. That
/// silence is why this is tested rather than assumed.
/// </para>
/// <para>
/// No network is involved: presigning is a local signature computation once the region is fixed.
/// </para>
/// </summary>
public sealed class MinioPresignedDownloadNameTests
{
    private const string Bucket = "deal-documents";
    private const string ObjectKey = "deal-documents/abc/term-sheet.pdf";

    private static MinioFileStorage CreateStorage()
    {
        IMinioClient client = new MinioClient()
            .WithEndpoint("localhost:9000")
            .WithCredentials("testaccesskey", "testsecretkeytestsecretkey")
            .WithRegion("us-east-1")
            .WithSSL(false)
            .Build();

        var options = Options.Create(new MinioOptions
        {
            PubEndpoint = "localhost:9000",
            AccessKey = "testaccesskey",
            SecretKey = "testsecretkeytestsecretkey",
            PubUseSsl = false,
        });

        return new MinioFileStorage(client, options, NullLogger<MinioFileStorage>.Instance);
    }

    private static async Task<string> PresignAsync(string? downloadFileName) =>
        await CreateStorage().GetPresignedUrl(
            ObjectKey, Bucket, 600, CancellationToken.None, downloadFileName);

    /// <summary>Reads one query parameter without pulling System.Web into the test project.</summary>
    private static string? QueryParam(string url, string name)
    {
        string query = new Uri(url).Query.TrimStart('?');
        foreach (string pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int split = pair.IndexOf('=');
            if (split > 0 && Uri.UnescapeDataString(pair[..split]) == name)
            {
                return Uri.UnescapeDataString(pair[(split + 1)..].Replace('+', ' '));
            }
        }

        return null;
    }

    private static string? Disposition(string url) => QueryParam(url, "response-content-disposition");

    [Fact]
    public async Task ADownloadName_BecomesAContentDispositionQueryParameter()
    {
        string url = await PresignAsync("term-sheet-2026-05-16.pdf");

        Disposition(url).ShouldBe("attachment; filename=\"term-sheet-2026-05-16.pdf\"");
    }

    [Fact]
    public async Task WithoutADownloadName_NoOverrideIsAdded()
    {
        string url = await PresignAsync(null);

        Disposition(url).ShouldBeNull();
    }

    /// <summary>
    /// The override has to be inside the signature, or anyone holding the link could rename the file
    /// the recipient saves. Two URLs that differ only by the name must therefore differ by signature.
    /// </summary>
    [Fact]
    public async Task TheDownloadName_IsCoveredByTheSignature()
    {
        string withName = await PresignAsync("term-sheet-2026-05-16.pdf");
        string withoutName = await PresignAsync(null);

        static string Signature(string url) => QueryParam(url, "X-Amz-Signature")!;

        Signature(withName).ShouldNotBeNullOrEmpty();
        Signature(withName).ShouldNotBe(Signature(withoutName));
    }

    /// <summary>
    /// Nothing user-supplied reaches this today, but the parameter is on a public abstraction: a quote
    /// would close the filename early and CR/LF could split the emitted header.
    /// </summary>
    [Fact]
    public async Task ANameWithHeaderBreakingCharacters_IsNeutralised()
    {
        string url = await PresignAsync("evil\".pdf\r\nX-Injected: 1;");

        string? disposition = Disposition(url);
        disposition.ShouldNotBeNull();
        disposition.ShouldBe("attachment; filename=\"evil_.pdf__X-Injected: 1_\"");
        disposition.ShouldNotContain("\r");
        disposition.ShouldNotContain("\n");
    }
}
