using System.Text.Json.Serialization;

namespace Graft.Core.Diagnostics;

/// <summary>
/// One recorded controller-side operation for failure diagnostics.
/// </summary>
public sealed class OperationLogEntry
{
    /// <summary>
    /// Gets the UTC timestamp when the operation was recorded.
    /// </summary>
    [JsonPropertyName("at")]
    public DateTimeOffset At { get; init; }

    /// <summary>
    /// Gets the action id (see <see cref="FailureSteps"/>).
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>
    /// Gets an optional short detail (selector, expected value, etc.).
    /// </summary>
    [JsonPropertyName("detail")]
    public string? Detail { get; init; }
}
