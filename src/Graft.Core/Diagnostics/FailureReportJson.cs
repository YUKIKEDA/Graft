using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graft.Core.Diagnostics;

/// <summary>
/// JSON helpers for <see cref="FailureReport"/> (exchange / logging form).
/// </summary>
public static class FailureReportJson
{
    /// <summary>
    /// Shared serializer options (camelCase property names via attributes, omit nulls).
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes a failure report to UTF-8 JSON text.
    /// </summary>
    /// <param name="report">Report to serialize.</param>
    /// <returns>JSON text.</returns>
    public static string Serialize(FailureReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, Options);
    }

    /// <summary>
    /// Deserializes a failure report from UTF-8 JSON text.
    /// </summary>
    /// <param name="json">JSON text.</param>
    /// <returns>The report.</returns>
    /// <exception cref="JsonException">JSON is invalid or missing required members.</exception>
    public static FailureReport Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<FailureReport>(json, Options)
            ?? throw new JsonException("FailureReport JSON deserialized to null.");
    }
}
