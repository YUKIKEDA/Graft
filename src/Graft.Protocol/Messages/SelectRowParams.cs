using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>selectRow</c>.
/// </summary>
public sealed class SelectRowParams : ElementTargetParams
{
    /// <summary>
    /// Gets the column Header key.
    /// </summary>
    [JsonPropertyName("columnKey")]
    public string? ColumnKey { get; init; }

    /// <summary>
    /// Gets the exact cell display text.
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; init; }
}
