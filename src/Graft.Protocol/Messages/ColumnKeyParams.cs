using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>clickColumnHeader</c>.
/// </summary>
public sealed class ColumnKeyParams : ElementTargetParams
{
    /// <summary>
    /// Gets the column Header key.
    /// </summary>
    [JsonPropertyName("columnKey")]
    public string? ColumnKey { get; init; }
}
