using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Reads a JSON null string as <see cref="string.Empty"/> and writes strings unchanged.
/// </summary>
/// <remarks>
/// A missing property stays <see langword="null"/> so callers can tell "omitted" from "present and empty".
/// </remarks>
public sealed class NullAsEmptyStringConverter : JsonConverter<string?>
{
    /// <inheritdoc />
    public override bool HandleNull => true;

    /// <inheritdoc />
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => string.Empty,
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            _ => throw new JsonException(),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
