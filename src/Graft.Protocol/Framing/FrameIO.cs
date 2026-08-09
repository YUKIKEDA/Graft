using System.Buffers.Binary;

namespace Graft.Protocol.Framing;

/// <summary>
/// Length-prefixed frame I/O: 4-byte little-endian length + payload.
/// </summary>
public static class FrameIO
{
    /// <summary>
    /// Size of the length prefix in bytes.
    /// </summary>
    public const int LengthPrefixSize = 4;

    /// <summary>
    /// Default maximum payload size (16 MiB). Screenshots use a follow-up binary frame later.
    /// </summary>
    public const int DefaultMaxPayloadBytes = 16 * 1024 * 1024;

    /// <summary>
    /// Writes a length-prefixed payload to <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Destination stream.</param>
    /// <param name="payload">Frame payload bytes.</param>
    /// <param name="maxPayloadBytes">Maximum allowed payload length.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the frame has been written.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the payload exceeds <paramref name="maxPayloadBytes"/>.</exception>
    public static async Task WriteAsync(
        Stream stream,
        ReadOnlyMemory<byte> payload,
        int maxPayloadBytes = DefaultMaxPayloadBytes,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (payload.Length > maxPayloadBytes)
        {
            throw new InvalidOperationException(
                $"Payload length {payload.Length} exceeds maximum {maxPayloadBytes}."
            );
        }

        var prefix = new byte[LengthPrefixSize];
        BinaryPrimitives.WriteInt32LittleEndian(prefix, payload.Length);
        await stream.WriteAsync(prefix, cancellationToken).ConfigureAwait(false);
        if (!payload.IsEmpty)
        {
            await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads one length-prefixed payload from <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">Source stream.</param>
    /// <param name="maxPayloadBytes">Maximum allowed payload length.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payload bytes (empty array when length is 0).</returns>
    /// <exception cref="InvalidOperationException">Thrown when the length is negative or exceeds <paramref name="maxPayloadBytes"/>.</exception>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends before the full frame is read.</exception>
    public static async Task<byte[]> ReadAsync(
        Stream stream,
        int maxPayloadBytes = DefaultMaxPayloadBytes,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);

        var prefix = new byte[LengthPrefixSize];
        await ReadExactAsync(stream, prefix, cancellationToken).ConfigureAwait(false);
        var length = BinaryPrimitives.ReadInt32LittleEndian(prefix);
        if (length < 0)
        {
            throw new InvalidOperationException($"Negative frame length: {length}.");
        }

        if (length > maxPayloadBytes)
        {
            throw new InvalidOperationException(
                $"Frame length {length} exceeds maximum {maxPayloadBytes}."
            );
        }

        if (length == 0)
        {
            return Array.Empty<byte>();
        }

        var payload = new byte[length];
        await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
        return payload;
    }

    private static async Task ReadExactAsync(
        Stream stream,
        Memory<byte> buffer,
        CancellationToken cancellationToken
    )
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = await stream
                .ReadAsync(buffer[offset..], cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                throw new EndOfStreamException(
                    $"Unexpected end of stream after {offset} of {buffer.Length} bytes."
                );
            }

            offset += read;
        }
    }
}
