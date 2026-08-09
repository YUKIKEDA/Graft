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
    /// <param name="timeout">Overall connect + handshake budget (both phases share this).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open, handshaken connection.</returns>
    /// <exception cref="GraftException">Connection, handshake, or overall timeout failed.</exception>
    public static async Task<AgentConnection> ConnectAsync(
        string pipeName,
        string token,
        TimeSpan timeout,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                "Timeout must be positive."
            );
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        var budgetToken = timeoutCts.Token;

        AgentConnection? connection = null;
        try
        {
            var stream = await ConnectPipeAsync(pipeName, budgetToken).ConfigureAwait(false);
            connection = new AgentConnection(stream);
            await connection.HandshakeAsync(token, budgetToken).ConfigureAwait(false);
            var result = connection;
            connection = null;
            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GraftException(
                GraftErrorCodes.ActionTimeout,
                $"Connect + handshake timed out after {timeout.TotalSeconds:0.###}s."
            );
        }
        finally
        {
            if (connection is not null)
            {
                await connection.DisposeAsync().ConfigureAwait(false);
            }
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

    /// <summary>
    /// Calls <c>setValue</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="value">Replacement text (empty string clears).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when setValue succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SetValueAsync(
        string automationId,
        string value,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(value);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SetValue,
                    Params = JsonSerializer.SerializeToElement(new { automationId, value }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "setValue failed.");
    }

    /// <summary>
    /// Calls <c>toggle</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when toggle succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task ToggleAsync(
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
                    Method = ProtocolMethods.Toggle,
                    Params = JsonSerializer.SerializeToElement(new { automationId }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "toggle failed.");
    }

    /// <summary>
    /// Calls <c>sendKeys</c> for the element with the given automation id.
    /// </summary>
    /// <param name="automationId">Target automation id.</param>
    /// <param name="text">Literal text to type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when sendKeys succeeds.</returns>
    /// <exception cref="GraftException">RPC failed.</exception>
    public async Task SendKeysAsync(
        string automationId,
        string text,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(automationId);
        ArgumentNullException.ThrowIfNull(text);
        ThrowIfDisposed();

        var response = await SendAsync(
                new RequestMessage
                {
                    V = ProtocolVersion.Current,
                    Id = NextId(),
                    Method = ProtocolMethods.SendKeys,
                    Params = JsonSerializer.SerializeToElement(new { automationId, text }),
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        EnsureOk(response, "sendKeys failed.");
    }

    /// <summary>
    /// Calls <c>screenshot</c> and reads the following raw PNG frame.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Screenshot meta and PNG bytes.</returns>
    /// <exception cref="GraftException">RPC failed or frame mismatch.</exception>
    public async Task<(ScreenshotResult Meta, byte[] PngBytes)> ScreenshotAsync(
        CancellationToken cancellationToken = default
    )
    {
        ThrowIfDisposed();

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
            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                "screenshot returned no result."
            );
        }

        var meta =
            resultElement.Deserialize<ScreenshotResult>(JsonMessageCodec.Options)
            ?? throw new GraftException(
                GraftErrorCodes.ActionFailed,
                "screenshot result deserialized to null."
            );

        byte[] pngBytes;
        try
        {
            pngBytes = await FrameIO
                .ReadAsync(_stream, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            throw new GraftException(
                GraftErrorCodes.PipeDisconnected,
                "Named pipe connection was lost while reading screenshot bytes.",
                ex
            );
        }

        if (pngBytes.Length != meta.ByteLength)
        {
            throw new GraftException(
                GraftErrorCodes.ActionFailed,
                $"screenshot byteLength mismatch: meta={meta.ByteLength}, frame={pngBytes.Length}."
            );
        }

        return (meta, pngBytes);
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
        CancellationToken cancellationToken
    )
    {
        var stream = new NamedPipeClientStream(
            ".",
            pipeName,
            PipeDirection.InOut,
            PipeOptions.Asynchronous
        );

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await stream.ConnectAsync(200, cancellationToken).ConfigureAwait(false);
                    return stream;
                }
                catch (Exception ex)
                    when (ex is TimeoutException or IOException or UnauthorizedAccessException)
                {
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
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
