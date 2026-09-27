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
internal sealed partial class AgentPipeServer : IDisposable
{
    private static readonly string AgentVersion = DescribeVersion(typeof(AgentPipeServer).Assembly);

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
        ArgumentException.ThrowIfNullOrWhiteSpace(connectToken);
        _pipeName = pipeName;
        _connectToken = connectToken;
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
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // A single misbehaving connection must never take down the accept loop.
                System.Diagnostics.Trace.TraceWarning($"Graft agent pipe: connection dropped after unexpected error: {ex}");
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

    private async Task HandleConnectionAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        var handshaken = false;

        while (!cancellationToken.IsCancellationRequested && server.IsConnected)
        {
            RequestMessage request;
            try
            {
                request = await JsonMessageCodec.ReadRequestAsync(server, cancellationToken).ConfigureAwait(false);
            }
            catch (EndOfStreamException)
            {
                break;
            }
            catch (IOException)
            {
                break;
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                // Malformed JSON, null envelope, or an invalid / oversized length prefix. The stream
                // position is no longer trustworthy, so drop this connection and accept a new one.
                System.Diagnostics.Trace.TraceWarning($"Graft agent pipe: dropping connection after malformed frame: {ex.Message}");
                break;
            }

            (ResponseMessage Response, bool CloseAfterWrite, byte[]? BinaryFollowUp) dispatched;
            try
            {
                dispatched = Dispatch(request, handshaken);
            }
            catch (Exception ex)
            {
                dispatched = (
                    Error(request.Id ?? string.Empty, GraftErrorCodes.ActionFailed, ex.Message),
                    CloseAfterWrite: false,
                    BinaryFollowUp: null
                );
            }

            var (response, closeAfterWrite, binaryFollowUp) = dispatched;
            if (response.Ok && request.Method == ProtocolMethods.Handshake)
            {
                handshaken = true;
            }

            try
            {
                await JsonMessageCodec.WriteResponseAsync(server, response, cancellationToken).ConfigureAwait(false);

                if (binaryFollowUp is { Length: > 0 })
                {
                    await FrameIO.WriteAsync(server, binaryFollowUp, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
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

    private static string DescribeVersion(System.Reflection.Assembly assembly) =>
        assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), inherit: false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()
            ?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";

    private bool IsTokenValid(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(token),
            System.Text.Encoding.UTF8.GetBytes(_connectToken)
        );
    }

    private static ResponseMessage Ok(string id, JsonElement? result = null) =>
        new()
        {
            V = ProtocolVersion.Current,
            Id = id,
            Ok = true,
            Result = result,
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
