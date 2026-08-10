using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Identity of a realized element returned by actions such as <c>scrollIntoView</c>.
/// </summary>
public sealed class ElementIdentity
{
    /// <summary>
    /// Gets the automation id (required for subsequent wire actions).
    /// </summary>
    [JsonPropertyName("automationId")]
    public required string AutomationId { get; init; }

    /// <summary>
    /// Gets the session-local runtime id when available.
    /// </summary>
    [JsonPropertyName("runtimeId")]
    public int? RuntimeId { get; init; }
}
