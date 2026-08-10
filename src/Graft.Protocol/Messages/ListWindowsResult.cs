using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Result of <c>listWindows</c>.
/// </summary>
public sealed class ListWindowsResult
{
    /// <summary>
    /// Gets the open windows in the target process.
    /// </summary>
    [JsonPropertyName("windows")]
    public required IReadOnlyList<WindowInfo> Windows { get; init; }
}
