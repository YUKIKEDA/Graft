using System.IO.Pipes;
using System.Text.Json;
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
        ClearGraftEnvironment();
        Agent.Stop();
    }

    public void Dispose()
    {
        Agent.Stop();
        ClearGraftEnvironment();
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
        StartAgent(token: "secret");

        await using var client = await ConnectAsync(_pipeName);
        var response = await SendHandshakeAsync(
            client,
            v: ProtocolVersion.Current,
            token: "secret"
        );

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
        StartAgent(token: "secret");

        await using var client = await ConnectAsync(_pipeName);
        var response = await SendHandshakeAsync(client, v: ProtocolVersion.Current, token: "wrong");

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
    /// </remarks>
    [Fact]
    public async Task Handshake_WithVersionMismatch_ReturnsProtocolVersionMismatch()
    {
        StartAgent(token: "secret");

        await using var client = await ConnectAsync(_pipeName);
        var response = await SendHandshakeAsync(client, v: 999, token: "secret");

        Assert.False(response.Ok);
        Assert.Equal(GraftErrorCodes.ProtocolVersionMismatch, response.Error?.Code);
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
        StartAgent(token: "secret");

        await using (var first = await ConnectAsync(_pipeName))
        {
            var firstResponse = await SendHandshakeAsync(
                first,
                v: ProtocolVersion.Current,
                token: "secret"
            );
            Assert.True(firstResponse.Ok);
        }

        await using var second = await ConnectAsync(_pipeName);
        var secondResponse = await SendHandshakeAsync(
            second,
            v: ProtocolVersion.Current,
            token: "secret",
            id: "2"
        );

        Assert.True(secondResponse.Ok);
        Assert.Equal("2", secondResponse.Id);
    }

    private void StartAgent(string token)
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, _pipeName);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, token);
        Agent.Start();
        Assert.True(Agent.IsRunning);
    }

    private static async Task<NamedPipeClientStream> ConnectAsync(string pipeName)
    {
        var client = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous
        );

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await client.ConnectAsync(200).ConfigureAwait(false);
                return client;
            }
            catch (Exception ex)
                when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                last = ex;
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        await client.DisposeAsync().ConfigureAwait(false);
        throw new TimeoutException($"Could not connect to pipe '{pipeName}'.", last);
    }

    private static async Task<ResponseMessage> SendHandshakeAsync(
        Stream stream,
        int v,
        string token,
        string id = "1"
    )
    {
        using var paramsDoc = JsonDocument.Parse(
            $"{{\"token\":{JsonSerializer.Serialize(token)}}}"
        );
        var request = new RequestMessage
        {
            V = v,
            Id = id,
            Method = ProtocolMethods.Handshake,
            Params = paramsDoc.RootElement.Clone(),
        };

        await JsonMessageCodec.WriteRequestAsync(stream, request).ConfigureAwait(false);
        return await JsonMessageCodec.ReadResponseAsync(stream).ConfigureAwait(false);
    }

    private static void ClearGraftEnvironment()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, null);
    }
}
