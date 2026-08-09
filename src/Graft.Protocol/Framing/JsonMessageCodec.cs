using System.Text.Json;
using Graft.Protocol.Messages;

namespace Graft.Protocol.Framing;

/// <summary>
/// JSON serialization helpers for request/response envelopes.
/// </summary>
public static class JsonMessageCodec
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static byte[] EncodeRequest(RequestMessage message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, Options);

    public static byte[] EncodeResponse(ResponseMessage message) =>
        JsonSerializer.SerializeToUtf8Bytes(message, Options);

    public static RequestMessage DecodeRequest(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<RequestMessage>(utf8Json, Options)
        ?? throw new InvalidOperationException("Request JSON deserialized to null.");

    public static ResponseMessage DecodeResponse(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<ResponseMessage>(utf8Json, Options)
        ?? throw new InvalidOperationException("Response JSON deserialized to null.");

    public static async Task WriteRequestAsync(
        Stream stream,
        RequestMessage message,
        CancellationToken cancellationToken = default
    )
    {
        var payload = EncodeRequest(message);
        await FrameIO
            .WriteAsync(stream, payload, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task WriteResponseAsync(
        Stream stream,
        ResponseMessage message,
        CancellationToken cancellationToken = default
    )
    {
        var payload = EncodeResponse(message);
        await FrameIO
            .WriteAsync(stream, payload, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<RequestMessage> ReadRequestAsync(
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        var payload = await FrameIO
            .ReadAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return DecodeRequest(payload);
    }

    public static async Task<ResponseMessage> ReadResponseAsync(
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        var payload = await FrameIO
            .ReadAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return DecodeResponse(payload);
    }
}
