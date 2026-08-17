using System.Text.Json;
using Graft.Protocol.Messages;

namespace Graft.Protocol.Framing;

/// <summary>
/// JSON serialization helpers for request/response envelopes.
/// </summary>
public static class JsonMessageCodec
{
    /// <summary>
    /// Shared serializer options for wire JSON.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes a request envelope to UTF-8 JSON bytes.
    /// </summary>
    /// <param name="message">Request message.</param>
    /// <returns>UTF-8 JSON payload.</returns>
    public static byte[] EncodeRequest(RequestMessage message) => JsonSerializer.SerializeToUtf8Bytes(message, Options);

    /// <summary>
    /// Serializes a response envelope to UTF-8 JSON bytes.
    /// </summary>
    /// <param name="message">Response message.</param>
    /// <returns>UTF-8 JSON payload.</returns>
    public static byte[] EncodeResponse(ResponseMessage message) => JsonSerializer.SerializeToUtf8Bytes(message, Options);

    /// <summary>
    /// Deserializes a request envelope from UTF-8 JSON.
    /// </summary>
    /// <param name="utf8Json">UTF-8 JSON bytes.</param>
    /// <returns>The request message.</returns>
    public static RequestMessage DecodeRequest(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<RequestMessage>(utf8Json, Options) ?? throw new InvalidOperationException("Request JSON deserialized to null.");

    /// <summary>
    /// Deserializes a response envelope from UTF-8 JSON.
    /// </summary>
    /// <param name="utf8Json">UTF-8 JSON bytes.</param>
    /// <returns>The response message.</returns>
    public static ResponseMessage DecodeResponse(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<ResponseMessage>(utf8Json, Options) ?? throw new InvalidOperationException("Response JSON deserialized to null.");

    /// <summary>
    /// Encodes and writes a length-prefixed request frame.
    /// </summary>
    /// <param name="stream">Destination stream.</param>
    /// <param name="message">Request message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the frame has been written.</returns>
    public static async Task WriteRequestAsync(Stream stream, RequestMessage message, CancellationToken cancellationToken = default)
    {
        var payload = EncodeRequest(message);
        await FrameIO.WriteAsync(stream, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Encodes and writes a length-prefixed response frame.
    /// </summary>
    /// <param name="stream">Destination stream.</param>
    /// <param name="message">Response message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the frame has been written.</returns>
    public static async Task WriteResponseAsync(Stream stream, ResponseMessage message, CancellationToken cancellationToken = default)
    {
        var payload = EncodeResponse(message);
        await FrameIO.WriteAsync(stream, payload, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads and decodes a length-prefixed request frame.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decoded request message.</returns>
    public static async Task<RequestMessage> ReadRequestAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var payload = await FrameIO.ReadAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return DecodeRequest(payload);
    }

    /// <summary>
    /// Reads and decodes a length-prefixed response frame.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decoded response message.</returns>
    public static async Task<ResponseMessage> ReadResponseAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var payload = await FrameIO.ReadAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return DecodeResponse(payload);
    }
}
