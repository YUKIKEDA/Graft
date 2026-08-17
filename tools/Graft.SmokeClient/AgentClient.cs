using System.IO.Pipes;
using System.Text.Json;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.SmokeClient;

/// <summary>
/// Minimal named-pipe client for handshake, getTree, screenshot, and invoke.
/// </summary>
internal sealed class AgentClient : IAsyncDisposable
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private readonly NamedPipeClientStream _stream;
    private int _nextId = 1;

    private AgentClient(NamedPipeClientStream stream)
    {
        _stream = stream;
    }

    public static async Task<AgentClient> ConnectAsync(string pipeName, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        var deadline = DateTime.UtcNow + timeout;
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var remaining = deadline - DateTime.UtcNow;
                var sliceMs = (int)Math.Clamp(remaining.TotalMilliseconds, 1, 200);
                await stream.ConnectAsync(sliceMs, cancellationToken).ConfigureAwait(false);
                return new AgentClient(stream);
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                last = ex;
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        await stream.DisposeAsync().ConfigureAwait(false);
        throw new SmokeException(GraftErrorCodes.PipeDisconnected, $"Could not connect to pipe '{pipeName}' within {timeout.TotalSeconds:0}s.", last);
    }

    public async Task HandshakeAsync(string token, CancellationToken cancellationToken)
    {
        using var paramsDoc = JsonDocument.Parse($"{{\"token\":{JsonSerializer.Serialize(token)}}}");
        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Handshake,
                    Params = paramsDoc.RootElement.Clone(),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "Handshake failed.");
    }

    public async Task<GetTreeResult> GetTreeAsync(CancellationToken cancellationToken)
    {
        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.GetTree,
                    Params = JsonSerializer.SerializeToElement(new { depth = 25, maxNodes = 2000 }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "getTree failed.");
        if (response.Result is not { } resultElement)
        {
            throw new SmokeException(GraftErrorCodes.ActionFailed, "getTree returned no result.");
        }

        return resultElement.Deserialize<GetTreeResult>(JsonMessageCodec.Options)
            ?? throw new SmokeException(GraftErrorCodes.ActionFailed, "getTree result deserialized to null.");
    }

    public async Task<(ScreenshotResult Meta, byte[] PngBytes)> ScreenshotAsync(CancellationToken cancellationToken)
    {
        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Screenshot,
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "screenshot failed.");
        if (response.Result is not { } resultElement)
        {
            throw new SmokeException(GraftErrorCodes.ActionFailed, "screenshot returned no result.");
        }

        var meta =
            resultElement.Deserialize<ScreenshotResult>(JsonMessageCodec.Options)
            ?? throw new SmokeException(GraftErrorCodes.ActionFailed, "screenshot result deserialized to null.");

        var pngBytes = await FrameIO.ReadAsync(_stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (pngBytes.Length != meta.ByteLength)
        {
            throw new SmokeException(
                GraftErrorCodes.ActionFailed,
                $"screenshot byteLength mismatch: meta={meta.ByteLength}, frame={pngBytes.Length}."
            );
        }

        if (pngBytes.Length < PngSignature.Length || !pngBytes.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            throw new SmokeException(GraftErrorCodes.ActionFailed, "screenshot raw frame does not start with a PNG signature.");
        }

        return (meta, pngBytes);
    }

    public async Task InvokeAsync(string automationId, CancellationToken cancellationToken)
    {
        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.Invoke,
                    Params = JsonSerializer.SerializeToElement(new { automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "invoke failed.");
    }

    public async ValueTask DisposeAsync() => await _stream.DisposeAsync().ConfigureAwait(false);

    private async Task<ResponseMessage> SendAsync(RequestMessage request, CancellationToken cancellationToken)
    {
        await JsonMessageCodec.WriteRequestAsync(_stream, request, cancellationToken).ConfigureAwait(false);
        return await JsonMessageCodec.ReadResponseAsync(_stream, cancellationToken).ConfigureAwait(false);
    }

    private string NextId() => Interlocked.Increment(ref _nextId).ToString();

    private static void EnsureOk(ResponseMessage response, string fallbackMessage)
    {
        if (response.Ok)
        {
            return;
        }

        var code = response.Error?.Code ?? GraftErrorCodes.ActionFailed;
        var message = response.Error?.Message ?? fallbackMessage;
        throw new SmokeException(code, message);
    }
}
