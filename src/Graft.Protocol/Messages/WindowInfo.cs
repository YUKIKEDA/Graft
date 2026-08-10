using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Session-local window descriptor returned by <c>listWindows</c>.
/// </summary>
public sealed class WindowInfo
{
    /// <summary>
    /// Gets the session-local window id (not stable across process restarts).
    /// </summary>
    [JsonPropertyName("windowId")]
    public int WindowId { get; init; }

    /// <summary>
    /// Gets the window title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Gets the automation id when set; otherwise an empty string.
    /// </summary>
    [JsonPropertyName("automationId")]
    public required string AutomationId { get; init; }

    /// <summary>
    /// Gets a value indicating whether the window is shown as a modal dialog.
    /// </summary>
    [JsonPropertyName("isModal")]
    public bool IsModal { get; init; }

    /// <summary>
    /// Gets a value indicating whether the window is the active window.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }
}
