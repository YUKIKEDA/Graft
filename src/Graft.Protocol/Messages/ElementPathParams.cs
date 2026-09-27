using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>selectMenu</c> and <c>selectTree</c>.
/// </summary>
public sealed class ElementPathParams
{
    /// <summary>
    /// Gets the root automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the slash-separated path.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }
}
