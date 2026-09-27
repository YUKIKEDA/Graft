using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>selectMany</c>.
/// </summary>
public sealed class SelectManyParams
{
    /// <summary>
    /// Gets the target automation id.
    /// </summary>
    [JsonPropertyName("automationId")]
    public string? AutomationId { get; init; }

    /// <summary>
    /// Gets the item indexes. An empty list clears the selection.
    /// </summary>
    [JsonPropertyName("indexes")]
    public IReadOnlyList<int>? Indexes { get; init; }
}
