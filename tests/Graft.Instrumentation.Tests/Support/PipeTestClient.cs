using System.IO.Pipes;
using System.Text.Json;
using Graft.Instrumentation;
using Graft.Protocol;
using Graft.Protocol.Framing;
using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tests;

/// <summary>
/// Named-pipe client shared by dispatch tests: connect, handshake, and one request/response exchange.
/// </summary>
internal static class PipeTestClient
{
    internal const string Token = "secret";

    internal static void ClearEnvironment()
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, null);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, null);
    }

    internal static void Start(string pipeName, string token = Token)
    {
        Environment.SetEnvironmentVariable(GraftEnvironment.Enable, "1");
        Environment.SetEnvironmentVariable(GraftEnvironment.PipeName, pipeName);
        Environment.SetEnvironmentVariable(GraftEnvironment.ConnectToken, token);
        Agent.Start();
        Assert.True(Agent.IsRunning);
    }

    internal static async Task<NamedPipeClientStream> ConnectAsync(string pipeName)
    {
        var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        Exception? last = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await client.ConnectAsync(200).ConfigureAwait(false);
                return client;
            }
            catch (Exception ex) when (ex is TimeoutException or IOException or UnauthorizedAccessException)
            {
                last = ex;
                await Task.Delay(50).ConfigureAwait(false);
            }
        }

        await client.DisposeAsync().ConfigureAwait(false);
        throw new TimeoutException($"Could not connect to pipe '{pipeName}'.", last);
    }

    internal static Task<ResponseMessage> HandshakeAsync(Stream stream, string token = Token, int version = ProtocolVersion.Current, string id = "1")
    {
        using var paramsDoc = JsonDocument.Parse($"{{\"token\":{JsonSerializer.Serialize(token)}}}");
        return SendAsync(
            stream,
            new RequestMessage
            {
                V = version,
                Id = id,
                Method = ProtocolMethods.Handshake,
                Params = paramsDoc.RootElement.Clone(),
            }
        );
    }

    internal static async Task<ResponseMessage> SendAsync(Stream stream, RequestMessage request)
    {
        var (response, _) = await ExchangeAsync(stream, request).ConfigureAwait(false);
        return response;
    }

    /// <summary>
    /// Writes one request and reads the JSON response. A successful screenshot also reads the PNG frame.
    /// </summary>
    internal static async Task<(ResponseMessage Response, byte[]? Binary)> ExchangeAsync(Stream stream, RequestMessage request)
    {
        await JsonMessageCodec.WriteRequestAsync(stream, request).ConfigureAwait(false);
        var response = await JsonMessageCodec.ReadResponseAsync(stream).ConfigureAwait(false);
        if (response.Ok && request.Method == ProtocolMethods.Screenshot)
        {
            var binary = await FrameIO.ReadAsync(stream).ConfigureAwait(false);
            return (response, binary);
        }

        return (response, null);
    }
}
