using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for arming an OpenFile, SaveFile, or OpenFolder seam with a path.
/// </summary>
public sealed class DialogPathParams
{
    /// <summary>
    /// Gets the path the seam returns.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}
