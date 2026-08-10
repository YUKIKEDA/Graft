using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Phase 1 visual-tree node returned by <c>getTree</c>.
/// </summary>
public sealed class TreeNode
{
    /// <summary>
    /// Gets the session-local runtime handle (not a stable test selector).
    /// </summary>
    [JsonPropertyName("runtimeId")]
    public int RuntimeId { get; init; }

    /// <summary>
    /// Gets the control type label (e.g. <c>Button</c>, <c>Window</c>).
    /// </summary>
    [JsonPropertyName("controlType")]
    public required string ControlType { get; init; }

    /// <summary>
    /// Gets the display name (Automation Name, content, or type fallback).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the automation id when set; otherwise an empty string.
    /// </summary>
    [JsonPropertyName("automationId")]
    public required string AutomationId { get; init; }

    /// <summary>
    /// Gets bounds in window-client logical DIP.
    /// </summary>
    [JsonPropertyName("bounds")]
    public required ElementBounds Bounds { get; init; }

    /// <summary>
    /// Gets a value indicating whether the element is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether the element is visible.
    /// </summary>
    [JsonPropertyName("visible")]
    public bool Visible { get; init; }

    /// <summary>
    /// Gets a value indicating whether the element is focused.
    /// </summary>
    [JsonPropertyName("focused")]
    public bool Focused { get; init; }

    /// <summary>
    /// Gets selection state for selection-capable items; <see langword="null"/> when not applicable.
    /// </summary>
    [JsonPropertyName("selected")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Selected { get; init; }

    /// <summary>
    /// Gets expand/collapse state for expandable elements; <see langword="null"/> when not applicable.
    /// </summary>
    [JsonPropertyName("expanded")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Expanded { get; init; }

    /// <summary>
    /// Gets child nodes in visual-tree order.
    /// </summary>
    [JsonPropertyName("children")]
    public required IReadOnlyList<TreeNode> Children { get; init; }
}
