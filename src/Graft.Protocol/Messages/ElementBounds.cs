using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Element rectangle in the target window's client logical coordinates (DIP).
/// </summary>
public sealed class ElementBounds
{
    /// <summary>
    /// Gets the X offset from the window client origin.
    /// </summary>
    [JsonPropertyName("x")]
    public double X { get; init; }

    /// <summary>
    /// Gets the Y offset from the window client origin.
    /// </summary>
    [JsonPropertyName("y")]
    public double Y { get; init; }

    /// <summary>
    /// Gets the width in DIP.
    /// </summary>
    [JsonPropertyName("width")]
    public double Width { get; init; }

    /// <summary>
    /// Gets the height in DIP.
    /// </summary>
    [JsonPropertyName("height")]
    public double Height { get; init; }
}
