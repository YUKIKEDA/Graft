using Graft.Protocol.Framing;

namespace Graft.Protocol.Tests;

public sealed class FrameIOTests
{
    /// <summary>
    /// WriteAsync/ReadAsync round-trips a non-empty payload on a MemoryStream.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Empty MemoryStream
    /// - UTF-8 payload bytes for "hello-graft"
    ///
    /// Steps:
    /// - FrameIO.WriteAsync then seek to 0
    /// - FrameIO.ReadAsync
    ///
    /// Expected:
    /// - Read bytes equal the original payload
    /// </remarks>
    [Fact]
    public async Task WriteThenRead_RoundTripsPayload()
    {
        await using var stream = new MemoryStream();
        var payload = "hello-graft"u8.ToArray();

        await FrameIO.WriteAsync(stream, payload);
        stream.Position = 0;

        var read = await FrameIO.ReadAsync(stream);
        Assert.Equal(payload, read);
    }

    /// <summary>
    /// Empty payloads are valid frames (length prefix 0).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Empty MemoryStream
    /// - Empty payload
    ///
    /// Steps:
    /// - WriteAsync empty payload, seek to 0, ReadAsync
    ///
    /// Expected:
    /// - Read result is an empty byte array
    /// </remarks>
    [Fact]
    public async Task WriteThenRead_EmptyPayload_Succeeds()
    {
        await using var stream = new MemoryStream();

        await FrameIO.WriteAsync(stream, ReadOnlyMemory<byte>.Empty);
        stream.Position = 0;

        var read = await FrameIO.ReadAsync(stream);
        Assert.Empty(read);
    }

    /// <summary>
    /// WriteAsync rejects payloads larger than maxPayloadBytes.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Empty MemoryStream
    /// - 8-byte payload with maxPayloadBytes = 4
    ///
    /// Steps:
    /// - Call FrameIO.WriteAsync
    ///
    /// Expected:
    /// - Throws InvalidOperationException
    /// </remarks>
    [Fact]
    public async Task Write_WhenPayloadExceedsMax_Throws()
    {
        await using var stream = new MemoryStream();
        var payload = new byte[8];

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await FrameIO.WriteAsync(stream, payload, maxPayloadBytes: 4));
    }

    /// <summary>
    /// ReadAsync rejects frames whose declared length exceeds maxPayloadBytes.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Stream containing a valid 16-byte frame written with default max
    /// - Read with maxPayloadBytes = 8
    ///
    /// Steps:
    /// - Write 16-byte frame, seek to 0
    /// - ReadAsync with a smaller max
    ///
    /// Expected:
    /// - Throws InvalidOperationException
    /// </remarks>
    [Fact]
    public async Task Read_WhenLengthExceedsMax_Throws()
    {
        await using var stream = new MemoryStream();
        await FrameIO.WriteAsync(stream, new byte[16]);
        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await FrameIO.ReadAsync(stream, maxPayloadBytes: 8));
    }
}
