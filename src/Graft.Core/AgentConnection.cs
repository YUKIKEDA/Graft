using System.IO.Pipes;
using System.Text.Json;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Core;

/// <summary>
/// Low-level named-pipe session to an instrumented agent (Connect + Handshake + RPC).
/// </summary>
/// <remarks>
/// Prefer <c>Application.LaunchAsync</c> (M2 Batch 2+) for the documented main path.
/// This type is the Connect / wire surface used by Launch and advanced callers.
/// </remarks>
public sealed class AgentConnection : IAsyncDisposable
{
    private readonly NamedPipeClientStream _stream;
    private int _nextId = 1;
    private bool _disposed;

    private AgentConnection(NamedPipeClientStream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Connects to the agent pipe and completes Handshake.
    /// </summary>
    /// <param name="pipeName">Named pipe name (<c>GRAFT_PIPE_NAME</c>).</param>
    /// <param name="token">Connect token (<c>GRAFT_CONNECT_TOKEN</c>).</param>
    /// <param name="timeout">Overall connect + handshake budget.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open, handshaken connection.</returns>
    /// <exception cref="GraftException">Connection or handshake failed.</exception>
    public static async Task<AgentConnection> ConnectAsync(
        string pipeName,
        string token,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var stream = await ConnectPipeAsync(pipeName, timeout, cancellationToken)
            .ConfigureAwait(false);
        var connection = new AgentConnection(stream);
        try
        {
            await connection.HandshakeAsync(token, cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Calls <c>getTree</c> with default depth / maxNodes limits.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tree result from the agent.</returns>
    /// <exception cref="GraftException">RPC failed or result missing.</exception>
    public async Task<GetTreeResult> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
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
            throw new GraftException(GraftErrorCodes.ActionFailed, "getTree returned no result.");
        }

        return resultElement.Deserialize<GetTreeResult>(JsonMessageCodec.Options)
            ?? throw new GraftException(
                GraftErrorCodes.ActionFailed,
                "getTree result deserialized to null."
            );
    }

    /// <summary>
    /// Calls <c>invoke</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when invoke succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task InvokeAsync(
        string automationId,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ThrowIfDisposed();

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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _stream.DisposeAsync().ConfigureAwait(false);
    }

    private async Task HandshakeAsync(string token, CancellationToken cancellationToken)
    {
        using var paramsDoc = JsonDocument.Parse(
            $"{{\"token\":{JsonSerializer.Serialize(token)}}}"
        );
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

    private static async Task<NamedPipeClientStream> ConnectPipeAsync(
        string pipeName,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        var stream = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous
        );

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
                return stream;
            }
            catch (Exception ex)
                when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                last = ex;
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        await stream.DisposeAsync().ConfigureAwait(false);
        throw new GraftException(
            GraftErrorCodes.PipeDisconnected,
            $"Could not connect to pipe '{pipeName}' within {timeout.TotalSeconds:0}s.",
            last
        );
    }

    private async Task<ResponseMessage> SendAsync(
        RequestMessage request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await JsonMessageCodec
                .WriteRequestAsync(_stream, request, cancellationToken)
                .ConfigureAwait(false);
            return await JsonMessageCodec
                .ReadResponseAsync(_stream, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            throw new GraftException(
                GraftErrorCodes.PipeDisconnected,
                "Named pipe connection was lost.",
                ex
            );
        }
        catch (ObjectDisposedException ex)
        {
            throw new GraftException(
                GraftErrorCodes.PipeDisconnected,
                "Named pipe connection was disposed.",
                ex
            );
        }
    }

    private string NextId() => Interlocked.Increment(ref _nextId).ToString();

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private static void EnsureOk(ResponseMessage response, string fallbackMessage)
    {
        if (response.Ok)
        {
            return;
        }

        var code = response.Error?.Code ?? GraftErrorCodes.ActionFailed;
        var message = response.Error?.Message ?? fallbackMessage;
        throw new GraftException(code, message);
    }
}
