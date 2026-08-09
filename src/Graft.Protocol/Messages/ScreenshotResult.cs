using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Success JSON payload for <c>screenshot</c> (raw PNG follows as the next frame).
/// </summary>
public sealed class ScreenshotResult
{
    /// <summary>
    /// Gets the image format (Phase 1: <c>png</c>).
    /// </summary>
    [JsonPropertyName("format")]
    public required string Format { get; init; }

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    [JsonPropertyName("width")]
    public int Width { get; init; }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    [JsonPropertyName("height")]
    public int Height { get; init; }

    /// <summary>
    /// Gets the length of the following raw frame in bytes.
    /// </summary>
    [JsonPropertyName("byteLength")]
    public int ByteLength { get; init; }
}
