using System.IO.Compression;
using System.Net;
using System.Text;
using DevStart.Infrastructure.PatentRegistry;
using DevStart.UnitTests.TestSupport;
using Microsoft.Extensions.Options;
using Shouldly;

namespace DevStart.UnitTests.Infrastructure.PatentRegistry;

/// <summary>
/// The download side of the register load. Everything here is a refusal path, because that is where
/// the risk is: the URL is configuration, the file behind it is written by somebody else, and the job
/// runs unattended. Each refusal must be cheap, loud and specific — a load that fails leaves the
/// previously loaded rows serving reads, so refusing is always better than half-reading.
/// </summary>
public sealed class RospatentDumpClientTests
{
    private const int Cap = 4096;

    [Fact]
    public async Task Download_ReadsAPlainCsv()
    {
        RospatentDumpClient client = CreateClient(Csv("Регистрационный номер;Название"));

        string csv = await client.DownloadCsvAsync("https://fips.test/programs.csv", default);

        csv.ShouldBe("Регистрационный номер;Название");
    }

    [Fact]
    public async Task Download_ReadsTheCsvInsideAZip()
    {
        RospatentDumpClient client = CreateClient(Zip(("programs.csv", "номер;название")));

        string csv = await client.DownloadCsvAsync("https://fips.test/programs.zip", default);

        csv.ShouldBe("номер;название");
    }

    [Fact]
    public async Task Download_RefusesWhenTheDeclaredLengthExceedsTheCap()
    {
        // Refused on the header, before a byte of the body is read.
        RospatentDumpClient client = CreateClient(new byte[Cap + 1]);

        InvalidDataException error = await Should.ThrowAsync<InvalidDataException>(
            () => client.DownloadCsvAsync("https://fips.test/huge.csv", default));

        error.Message.ShouldContain(Cap.ToString());
    }

    [Fact]
    public async Task Download_RefusesWhenTheBodyOutgrowsTheCapMidStream()
    {
        // No Content-Length: a chunked response can announce nothing and still deliver gigabytes, so
        // the header check cannot be the only guard.
        RospatentDumpClient client = CreateClient(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new NonSeekableStream(new byte[Cap * 2])),
            });

        await Should.ThrowAsync<InvalidDataException>(
            () => client.DownloadCsvAsync("https://fips.test/chunked.csv", default));
    }

    [Fact]
    public async Task Download_RefusesAZipWithoutACsv()
    {
        RospatentDumpClient client = CreateClient(Zip(("readme.txt", "нет тут выгрузки")));

        InvalidDataException error = await Should.ThrowAsync<InvalidDataException>(
            () => client.DownloadCsvAsync("https://fips.test/programs.zip", default));

        error.Message.ShouldContain("CSV");
    }

    [Fact]
    public async Task Download_RefusesAZipThatDecompressesBeyondTheCap()
    {
        // The zip-bomb case: a few hundred bytes on the wire, far more than the cap once expanded.
        // Highly compressible content on purpose — that is exactly what makes the attack cheap.
        byte[] archive = Zip(("programs.csv", new string('a', Cap * 50)));
        archive.Length.ShouldBeLessThan(Cap);

        RospatentDumpClient client = CreateClient(archive);

        InvalidDataException error = await Should.ThrowAsync<InvalidDataException>(
            () => client.DownloadCsvAsync("https://fips.test/bomb.zip", default));

        error.Message.ShouldContain(Cap.ToString());
    }

    [Fact]
    public async Task Download_RefusesBytesThatAreNotUtf8()
    {
        // windows-1251 «Ромашка»: a lenient decode would turn every holder name into replacement
        // characters that then read as data.
        byte[] cp1251 = [0xD0, 0xEE, 0xEC, 0xE0, 0xF8, 0xEA, 0xE0];

        RospatentDumpClient client = CreateClient(cp1251);

        InvalidDataException error = await Should.ThrowAsync<InvalidDataException>(
            () => client.DownloadCsvAsync("https://fips.test/1251.csv", default));

        error.Message.ShouldContain("UTF-8");
    }

    [Fact]
    public async Task Download_PropagatesAnHttpFailure()
    {
        RospatentDumpClient client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        await Should.ThrowAsync<HttpRequestException>(
            () => client.DownloadCsvAsync("https://fips.test/gone.csv", default));
    }

    private static RospatentDumpClient CreateClient(byte[] body) =>
        CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) });

    private static RospatentDumpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(
            new HttpClient(new CapturingHttpMessageHandler(responder)),
            Options.Create(new RospatentOptions { MaxDatasetBytes = Cap }));

    private static byte[] Csv(string content) => Encoding.UTF8.GetBytes(content);

    private static byte[] Zip(params (string Name, string Content)[] entries)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach ((string name, string content) in entries)
            {
                // No BOM: the client hands the bytes through untouched, and stripping a preamble is
                // the parser's job — the test should not smuggle one in.
                using StreamWriter writer = new(
                    archive.CreateEntry(name).Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                writer.Write(content);
            }
        }

        return buffer.ToArray();
    }

    /// <summary>A body whose size cannot be known in advance — what a chunked response looks like.</summary>
    private sealed class NonSeekableStream(byte[] bytes) : Stream
    {
        private readonly MemoryStream _inner = new(bytes);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
