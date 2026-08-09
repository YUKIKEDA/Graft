using System.IO.Pipes;
using System.Text.Json;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Pipe;

#if GRAFT_TEST

/// <summary>
/// Named-pipe listener for a single client with reconnect after disconnect.
/// </summary>
/// <remarks>
/// Uses <see cref="PipeOptions.CurrentUserOnly"/> (same-user ACL). At most one
/// <see cref="NamedPipeServerStream"/> instance is created at a time so a second
/// client cannot complete a connection while the first is active.
/// </remarks>
internal sealed class AgentPipeServer : IDisposable
{
    private readonly string _pipeName;
    private readonly string _connectToken;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private bool _disposed;

    /// <summary>
    /// Initializes and starts the accept loop.
    /// </summary>
    /// <param name="pipeName">Pipe name (without <c>\\.\pipe\</c> prefix).</param>
    /// <param name="connectToken">Expected handshake token.</param>
    public AgentPipeServer(string pipeName, string connectToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pipeName);
        _pipeName = pipeName;
        _connectToken = connectToken ?? string.Empty;
        _loop = RunAsync(_cts.Token);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        try
        {
            _ = _loop.Wait(TimeSpan.FromSeconds(5));
        }
        catch (AggregateException)
        {
            // Shutdown races with accept/read cancellation.
        }

        _cts.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = CreateServer();
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                await HandleConnectionAsync(server, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException)
            {
                // Client disconnected or pipe broken; accept again.
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            finally
            {
                if (server is not null)
                {
                    await server.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }

    private NamedPipeServerStream CreateServer() =>
        new(
            _pipeName,
            PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly
        );

    private async Task HandleConnectionAsync(
        NamedPipeServerStream server,
        CancellationToken cancellationToken
    )
    {
        var handshaken = false;

        while (!cancellationToken.IsCancellationRequested && server.IsConnected)
        {
            RequestMessage request;
            try
            {
                request = await JsonMessageCodec
                    .ReadRequestAsync(server, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (EndOfStreamException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }

            var (response, closeAfterWrite) = Dispatch(request, handshaken);
            if (response.Ok && request.Method == ProtocolMethods.Handshake)
            {
                handshaken = true;
            }

            try
            {
                await JsonMessageCodec
                    .WriteResponseAsync(server, response, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (IOException)
            {
                break;
            }

            if (closeAfterWrite)
            {
                break;
            }
        }
    }

    private (ResponseMessage Response, bool CloseAfterWrite) Dispatch(
        RequestMessage request,
        bool handshaken
    )
    {
        if (request.V != ProtocolVersion.Current)
        {
            return (
                Error(
                    request.Id,
                    GraftErrorCodes.ProtocolVersionMismatch,
                    $"Protocol version mismatch. Agent expects v={ProtocolVersion.Current}."
                ),
                CloseAfterWrite: true
            );
        }

        if (!handshaken)
        {
            if (request.Method != ProtocolMethods.Handshake)
            {
                return (
                    Error(
                        request.Id,
                        GraftErrorCodes.HandshakeRejected,
                        "Handshake is required before other methods."
                    ),
                    CloseAfterWrite: true
                );
            }

            var token = ReadToken(request.Params);
            if (!string.Equals(token, _connectToken, StringComparison.Ordinal))
            {
                return (
                    Error(request.Id, GraftErrorCodes.HandshakeRejected, "Connect token rejected."),
                    CloseAfterWrite: true
                );
            }

            return (Ok(request.Id), CloseAfterWrite: false);
        }

        if (request.Method == ProtocolMethods.Handshake)
        {
            var token = ReadToken(request.Params);
            if (!string.Equals(token, _connectToken, StringComparison.Ordinal))
            {
                return (
                    Error(request.Id, GraftErrorCodes.HandshakeRejected, "Connect token rejected."),
                    CloseAfterWrite: true
                );
            }

            return (Ok(request.Id), CloseAfterWrite: false);
        }

        // GetTree and other methods arrive in later batches.
        return (
            Error(
                request.Id,
                GraftErrorCodes.ActionFailed,
                $"Method '{request.Method}' is not implemented."
            ),
            CloseAfterWrite: false
        );
    }

    private static string ReadToken(JsonElement? paramsElement)
    {
        if (paramsElement is not { } element || element.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        if (!element.TryGetProperty("token", out var tokenProperty))
        {
            return string.Empty;
        }

        return tokenProperty.ValueKind switch
        {
            JsonValueKind.String => tokenProperty.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            _ => string.Empty,
        };
    }

    private static ResponseMessage Ok(string id) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = id,
            Ok = true,
        };

    private static ResponseMessage Error(string id, string code, string message) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = id,
            Ok = false,
            Error = new ErrorObject { Code = code, Message = message },
        };
}

#endif
