using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>getCellText</c>, <c>setCellValue</c>, and <c>selectCell</c>.
/// </summary>
public sealed class CellParams : ElementTargetParams
{
    /// <summary>
    /// Gets the zero-based row index.
    /// </summary>
    [JsonPropertyName("row")]
    public int? Row { get; init; }

    /// <summary>
    /// Gets the zero-based column index. Mutually exclusive with <see cref="ColumnKey"/>.
    /// </summary>
    [JsonPropertyName("column")]
    public int? Column { get; init; }

    /// <summary>
    /// Gets the column Header key. Mutually exclusive with <see cref="Column"/>.
    /// </summary>
    [JsonPropertyName("columnKey")]
    public string? ColumnKey { get; init; }

    /// <summary>
    /// Gets the replacement text for <c>setCellValue</c>. JSON null is an empty string.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonConverter(typeof(NullAsEmptyStringConverter))]
    public string? Value { get; init; }
}
