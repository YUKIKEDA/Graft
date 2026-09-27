using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>switchWindow</c>.
/// </summary>
public sealed class WindowIdParams
{
    /// <summary>
    /// Gets the session-local window id.
    /// </summary>
    [JsonPropertyName("windowId")]
    public int? WindowId { get; init; }
}
