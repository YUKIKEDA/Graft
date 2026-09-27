using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for a method that targets one element by automation id and optional runtime id.
/// </summary>
public sealed class ElementTargetParams
{
    /// <summary>
    /// Gets the automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the session-local runtime id.
    /// </summary>
    [JsonPropertyName("runtimeId")]
    public int? RuntimeId { get; init; }
}
