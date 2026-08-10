using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Result of <c>getCellText</c> (DataGrid Text cell display string).
/// </summary>
public sealed class CellTextResult
{
    /// <summary>
    /// Gets the cell display text.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}
