using System.IO.Pipes;
using Graft.Instrumentation;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tests;

public sealed class PipeHandshakeTests : IDisposable
{
    private readonly string _pipeName = "graft-hs-" + Guid.NewGuid().ToString("N");

    public PipeHandshakeTests()
    {
        PipeTestClient.ClearEnvironment();
        Agent.Stop();
    }

    public void Dispose()
    {
        Agent.Stop();
        PipeTestClient.ClearEnvironment();
    }

    /// <summary>
    /// Handshake succeeds when protocol version and connect token match.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with GRAFT_ENABLE=1, unique pipe name, token "secret"
    ///
    /// Steps:
    /// - Connect as named-pipe client
    /// - Send handshake with v=1 and matching token
    ///
    /// Expected:
    /// - Response ok=true with matching id
    /// </remarks>
    [Fact]
    public async Task Handshake_WithMatchingVersionAndToken_Succeeds()
    {
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        var response = await PipeTestClient.HandshakeAsync(client, token: "secret");

        Assert.True(response.Ok);
        Assert.Equal("1", response.Id);
        Assert.Null(response.Error);
    }

    /// <summary>
    /// Wrong connect token yields handshake.rejected and closes the connection.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with token "secret"
    ///
    /// Steps:
    /// - Connect and send handshake with token "wrong"
    ///
    /// Expected:
    /// - Response ok=false, code handshake.rejected
    /// </remarks>
    [Fact]
    public async Task Handshake_WithWrongToken_ReturnsHandshakeRejected()
    {
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        var response = await PipeTestClient.HandshakeAsync(client, token: "wrong");

        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.HandshakeRejected, response.Error?.Code);
    }

    /// <summary>
    /// A handshake with a missing or empty token is rejected.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with token "secret"
    ///
    /// Steps:
    /// - Connect and send handshake with token ""
    ///
    /// Expected:
    /// - Response ok=false, code handshake.rejected
    /// </remarks>
    [Fact]
    public async Task Handshake_WithEmptyToken_ReturnsHandshakeRejected()
    {
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        var response = await PipeTestClient.HandshakeAsync(client, token: "");

        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.HandshakeRejected, response.Error?.Code);
    }

    /// <summary>
    /// Protocol version mismatch yields protocol.versionMismatch.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with any token
    ///
    /// Steps:
    /// - Connect and send handshake with v=999
    ///
    /// Expected:
    /// - Response ok=false, code protocol.versionMismatch
    /// - Message names the client version and the agent package version
    /// </remarks>
    [Fact]
    public async Task Handshake_WithVersionMismatch_ReturnsProtocolVersionMismatch()
    {
        PipeTestClient.Start(_pipeName);

        await using var client = await PipeTestClient.ConnectAsync(_pipeName);
        var response = await PipeTestClient.HandshakeAsync(client, token: "secret", version: 999);

        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ProtocolVersionMismatch, response.Error?.Code);
        Assert.Contains("v=999", response.Error!.Message, StringComparison.Ordinal);
        Assert.Contains("Graft.Instrumentation ", response.Error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// After disconnect, a new client can handshake again.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with token "secret"
    ///
    /// Steps:
    /// - Connect, handshake successfully, dispose client
    /// - Connect again and handshake
    ///
    /// Expected:
    /// - Second handshake also ok=true
    /// </remarks>
    [Fact]
    public async Task Handshake_AfterDisconnect_AllowsReconnect()
    {
        PipeTestClient.Start(_pipeName);

        await using (var first = await PipeTestClient.ConnectAsync(_pipeName))
        {
            var firstResponse = await PipeTestClient.HandshakeAsync(first, token: "secret");
            Assert.True(firstResponse.Ok);
        }

        var (second, secondResponse) = await ReconnectAndHandshakeAsync(_pipeName, token: "secret", id: "2");
        await using var _ = second;

        Assert.True(secondResponse.Ok);
        Assert.Equal("2", secondResponse.Id);
    }

    /// <summary>
    /// A malformed JSON frame drops only that connection; the accept loop keeps serving new clients.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with token "secret"
    ///
    /// Steps:
    /// - Connect and send a length-prefixed frame whose body is not valid JSON
    /// - Connect again and handshake
    ///
    /// Expected:
    /// - The first connection is closed by the agent (read hits end of stream)
    /// - The second handshake is ok=true
    /// </remarks>
    [Fact]
    public async Task MalformedJsonFrame_DoesNotStopAcceptLoop()
    {
        PipeTestClient.Start(_pipeName);

        await using (var bad = await PipeTestClient.ConnectAsync(_pipeName))
        {
            await FrameIO.WriteAsync(bad, "{not json"u8.ToArray());
            await Assert.ThrowsAsync<EndOfStreamException>(() => FrameIO.ReadAsync(bad));
        }

        var (good, response) = await ReconnectAndHandshakeAsync(_pipeName, token: "secret");
        await using var _ = good;
        Assert.True(response.Ok);
    }

    /// <summary>
    /// A null JSON envelope or an oversized length prefix drops only that connection.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Agent started with token "secret"
    ///
    /// Steps:
    /// - Connect and send the frame body <c>null</c>; reconnect
    /// - Send a raw length prefix larger than the maximum payload; reconnect
    /// - Handshake on a fresh connection
    ///
    /// Expected:
    /// - Each bad connection is closed; the final handshake is ok=true
    /// </remarks>
    [Fact]
    public async Task NullEnvelopeAndOversizedFrame_DoNotStopAcceptLoop()
    {
        PipeTestClient.Start(_pipeName);

        await using (var nullEnvelope = await PipeTestClient.ConnectAsync(_pipeName))
        {
            await FrameIO.WriteAsync(nullEnvelope, "null"u8.ToArray());
            await Assert.ThrowsAsync<EndOfStreamException>(() => FrameIO.ReadAsync(nullEnvelope));
        }

        await using (var oversized = await PipeTestClient.ConnectAsync(_pipeName))
        {
            var prefix = new byte[FrameIO.LengthPrefixSize];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(prefix, FrameIO.DefaultMaxPayloadBytes + 1);
            await oversized.WriteAsync(prefix);
            await oversized.FlushAsync();
            await Assert.ThrowsAsync<EndOfStreamException>(() => FrameIO.ReadAsync(oversized));
        }

        var (good, response) = await ReconnectAndHandshakeAsync(_pipeName, token: "secret");
        await using var _ = good;
        Assert.True(response.Ok);
    }

    /// <summary>
    /// Connects and handshakes after a previous connection was dropped.
    /// </summary>
    /// <remarks>
    /// On Unix, .NET emulates named pipes with a listening socket that is closed and re-created when the
    /// agent recycles its single server instance, so a client that connects in that gap is reset. Windows
    /// named pipes queue the client instead. Retry so the reconnect tests are stable on both.
    /// </remarks>
    private static async Task<(NamedPipeClientStream Client, ResponseMessage Response)> ReconnectAndHandshakeAsync(
        string pipeName,
        string token,
        string id = "1"
    )
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (true)
        {
            var client = await PipeTestClient.ConnectAsync(pipeName).ConfigureAwait(false);
            try
            {
                var response = await PipeTestClient.HandshakeAsync(client, token, id: id).ConfigureAwait(false);
                return (client, response);
            }
            catch (Exception ex) when (ex is IOException && DateTime.UtcNow < deadline)
            {
                await client.DisposeAsync().ConfigureAwait(false);
                await Task.Delay(50).ConfigureAwait(false);
            }
        }
    }
}
