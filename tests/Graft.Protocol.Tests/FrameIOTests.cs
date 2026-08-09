using Graft.Protocol.Framing;

namespace Graft.Protocol.Tests;

public sealed class FrameIOTests
{
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

    [Fact]
    public async Task WriteThenRead_EmptyPayload_Succeeds()
    {
        await using var stream = new MemoryStream();

        await FrameIO.WriteAsync(stream, ReadOnlyMemory<byte>.Empty);
        stream.Position = 0;

        var read = await FrameIO.ReadAsync(stream);
        Assert.Empty(read);
    }

    [Fact]
    public async Task Write_WhenPayloadExceedsMax_Throws()
    {
        await using var stream = new MemoryStream();
        var payload = new byte[8];

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await FrameIO.WriteAsync(stream, payload, maxPayloadBytes: 4)
        );
    }

    [Fact]
    public async Task Read_WhenLengthExceedsMax_Throws()
    {
        await using var stream = new MemoryStream();
        await FrameIO.WriteAsync(stream, new byte[16]);
        stream.Position = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await FrameIO.ReadAsync(stream, maxPayloadBytes: 8)
        );
    }
}
