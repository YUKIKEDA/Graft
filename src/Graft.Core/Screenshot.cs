namespace Graft.Core;

/// <summary>
/// A captured PNG screenshot (window or element clip) plus size metadata.
/// </summary>
public sealed class Screenshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Screenshot"/> class.
    /// </summary>
    /// <param name="format">Image format (Phase 15: <c>png</c>).</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="pngBytes">PNG payload.</param>
    public Screenshot(string format, int width, int height, byte[] pngBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(format);
        ArgumentNullException.ThrowIfNull(pngBytes);
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(height),
                height,
                "Height must be positive."
            );
        }

        Format = format;
        Width = width;
        Height = height;
        PngBytes = pngBytes;
    }

    /// <summary>
    /// Gets the image format (Phase 15: <c>png</c>).
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the PNG payload.
    /// </summary>
    public byte[] PngBytes { get; }

    /// <summary>
    /// Writes <see cref="PngBytes"/> to <paramref name="path"/> (creates parent directories as needed).
    /// </summary>
    /// <param name="path">Destination file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the file is written.</returns>
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(full, PngBytes, cancellationToken).ConfigureAwait(false);
    }
}
