using DevStart.Application.PatentRegistry;
using Shouldly;

namespace DevStart.UnitTests.Application.PatentRegistry;

/// <summary>
/// The single guard behind both ways a dump arrives — the admin upload and the job's download. A
/// declared size is a claim by whoever sent the bytes; this is what actually counts them.
/// </summary>
public sealed class CappedStreamReaderTests
{
    [Fact]
    public async Task Read_ReturnsTheBytes_WhenTheStreamFitsExactlyWithinTheCap()
    {
        byte[] payload = new byte[256];
        Random.Shared.NextBytes(payload);

        byte[] read = await CappedStreamReader.ReadAsync(new MemoryStream(payload), 256, default);

        // The cap is a ceiling, not a strict bound: a file of exactly the allowed size is allowed.
        read.ShouldBe(payload);
    }

    [Fact]
    public async Task Read_Refuses_WhenTheStreamOutgrowsTheCapByOneByte()
    {
        InvalidDataException error = await Should.ThrowAsync<InvalidDataException>(
            () => CappedStreamReader.ReadAsync(new MemoryStream(new byte[257]), 256, default));

        error.Message.ShouldBe(CappedStreamReader.TooLargeMessage(256));
    }

    [Fact]
    public async Task Read_Refuses_WithoutBufferingPastTheCap()
    {
        // A stream that would never end: the reader must stop at the cap rather than at the source.
        await Should.ThrowAsync<InvalidDataException>(
            () => CappedStreamReader.ReadAsync(new EndlessStream(), 1024, default));
    }

    [Fact]
    public async Task Read_HandlesAnEmptyStream()
    {
        (await CappedStreamReader.ReadAsync(new MemoryStream([]), 16, default)).ShouldBeEmpty();
    }

    /// <summary>Yields zeros forever — the shape of a source that lies about how much it will send.</summary>
    private sealed class EndlessStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) => count;

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
