using System.Text.Json.Serialization;
using Graft.Protocol.Messages;

namespace Graft.Core.Diagnostics;

/// <summary>
/// Flat element snapshot stored on a tree diff. Children are separate entries.
/// </summary>
public sealed class TreeDiffNode
{
    /// <summary>
    /// Gets the control type label.
    /// </summary>
    [JsonPropertyName("controlType")]
    public required string ControlType { get; init; }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the automation id, or an empty string.
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
    /// Gets selection state when the control reports it.
    /// </summary>
    [JsonPropertyName("selected")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Selected { get; init; }

    /// <summary>
    /// Gets expand state when the control reports it.
    /// </summary>
    [JsonPropertyName("expanded")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Expanded { get; init; }

    /// <summary>
    /// Gets checked state when the control reports it.
    /// </summary>
    [JsonPropertyName("checked")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Checked { get; init; }

    /// <summary>
    /// Gets a string value when the control reports one.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; init; }

    /// <summary>
    /// Gets the open ToolTip text when reported.
    /// </summary>
    [JsonPropertyName("toolTip")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolTip { get; init; }

    internal static TreeDiffNode From(TreeNode node) =>
        new()
        {
            ControlType = node.ControlType,
            Name = node.Name,
            AutomationId = node.AutomationId,
            Bounds = node.Bounds,
            Enabled = node.Enabled,
            Visible = node.Visible,
            Focused = node.Focused,
            Selected = node.Selected,
            Expanded = node.Expanded,
            Checked = node.Checked,
            Value = node.Value,
            ToolTip = node.ToolTip,
        };
}
