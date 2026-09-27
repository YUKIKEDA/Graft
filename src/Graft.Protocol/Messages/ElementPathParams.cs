using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>selectMenu</c> and <c>selectTree</c>.
/// </summary>
public sealed class ElementPathParams : ElementTargetParams
{
    /// <summary>
    /// Gets the slash-separated path.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}
