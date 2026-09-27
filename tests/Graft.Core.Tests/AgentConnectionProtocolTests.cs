using System.IO.Pipes;
using System.Text.Json;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Core.Tests;

/// <summary>
/// Wire-level robustness of <see cref="AgentConnection"/> against a scripted fake agent.
/// </summary>
public sealed class AgentConnectionProtocolTests
{
    private const string Token = "secret";

    /// <summary>
    /// A response whose id does not match the request is rejected and the connection is poisoned.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake agent accepts the handshake, then answers the next request with a different id
    ///
    /// Steps:
    /// - ConnectAsync, then InvokeAsync
    /// - Call InvokeAsync again
    ///
    /// Expected:
    /// - First call throws GraftException pipe.disconnected mentioning the id mismatch
    /// - Second call fails fast with pipe.disconnected
    /// </remarks>
    [Fact]
    public async Task MismatchedResponseId_ThrowsPipeDisconnected_AndPoisonsConnection()
    {
        var pipeName = NewPipeName();
        var server = RunFakeAgentAsync(
            pipeName,
            async (stream, request) =>
            {
                await JsonMessageCodec.WriteResponseAsync(
                    stream,
                    new ResponseMessage
                    {
                        V = ProtocolVersion.Current,
                        Id = "999",
                        Ok = true,
                    }
                );
            }
        );

        await using var connection = await Application.ConnectAsync(pipeName, Token, TimeSpan.FromSeconds(5));

        var first = await Assert.ThrowsAsync<GraftException>(() => connection.InvokeAsync("A"));
        Assert.Equal(GraftErrorCodes.PipeDisconnected, first.Code);
        Assert.Contains("does not match", first.Message, StringComparison.Ordinal);

        var second = await Assert.ThrowsAsync<GraftException>(() => connection.InvokeAsync("B"));
        Assert.Equal(GraftErrorCodes.PipeDisconnected, second.Code);

        await connection.DisposeAsync();
        await server.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// A malformed response frame is normalized to GraftException instead of a raw JsonException.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake agent accepts the handshake, then answers with a frame that is not valid JSON
    ///
    /// Steps:
    /// - ConnectAsync, then InvokeAsync
    ///
    /// Expected:
    /// - GraftException with code pipe.disconnected (inner exception is JsonException)
    /// </remarks>
    [Fact]
    public async Task MalformedResponseFrame_ThrowsGraftException()
    {
        var pipeName = NewPipeName();
        var server = RunFakeAgentAsync(pipeName, async (stream, request) => await FrameIO.WriteAsync(stream, "{broken"u8.ToArray()));

        await using var connection = await Application.ConnectAsync(pipeName, Token, TimeSpan.FromSeconds(5));

        var ex = await Assert.ThrowsAsync<GraftException>(() => connection.InvokeAsync("A"));
        Assert.Equal(GraftErrorCodes.PipeDisconnected, ex.Code);
        Assert.IsAssignableFrom<JsonException>(ex.InnerException);

        await connection.DisposeAsync();
        await server.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// A response left unread by a cancelled call is discarded by the next call instead of being misattributed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake agent delays its answer to the first post-handshake request, answers later ones immediately
    ///
    /// Steps:
    /// - InvokeAsync("slow") with a token cancelled before the answer arrives
    /// - InvokeAsync("fast") without cancellation
    ///
    /// Expected:
    /// - First call throws OperationCanceledException
    /// - Second call succeeds (the stale ok=false response for "slow" is skipped)
    /// </remarks>
    [Fact]
    public async Task CancelledCall_StaleResponseIsDiscarded()
    {
        var pipeName = NewPipeName();
        var calls = 0;
        var server = RunFakeAgentAsync(
            pipeName,
            async (stream, request) =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                {
                    await Task.Delay(500);
                    await JsonMessageCodec.WriteResponseAsync(
                        stream,
                        new ResponseMessage
                        {
                            V = ProtocolVersion.Current,
                            Id = request.Id,
                            Ok = false,
                            Error = new ErrorObject { Code = GraftErrorCodes.ActionFailed, Message = "stale" },
                        }
                    );
                    return;
                }

                await JsonMessageCodec.WriteResponseAsync(
                    stream,
                    new ResponseMessage
                    {
                        V = ProtocolVersion.Current,
                        Id = request.Id,
                        Ok = true,
                    }
                );
            }
        );

        await using var connection = await Application.ConnectAsync(pipeName, Token, TimeSpan.FromSeconds(5));

        using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.InvokeAsync("slow", cts.Token));
        }

        await connection.InvokeAsync("fast");

        await connection.DisposeAsync();
        await server.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static string NewPipeName() => "graft-proto-" + Guid.NewGuid().ToString("N");

    /// <summary>
    /// Accepts one client, answers the handshake, then hands every later request to <paramref name="onRequest"/>.
    /// </summary>
    private static async Task RunFakeAgentAsync(string pipeName, Func<Stream, RequestMessage, Task> onRequest)
    {
        await using var server = new NamedPipeServerStream(
            pipeName,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly
        );
        await server.WaitForConnectionAsync();

        try
        {
            var handshake = await JsonMessageCodec.ReadRequestAsync(server);
            await JsonMessageCodec.WriteResponseAsync(
                server,
                new ResponseMessage
                {
                    V = ProtocolVersion.Current,
                    Id = handshake.Id,
                    Ok = true,
                }
            );

            while (true)
            {
                var request = await JsonMessageCodec.ReadRequestAsync(server);
                await onRequest(server, request);
            }
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException)
        {
            // Client disconnected.
        }
    }
}
