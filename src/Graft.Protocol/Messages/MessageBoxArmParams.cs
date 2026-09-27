using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>armMessageBox</c>.
/// </summary>
public sealed class MessageBoxArmParams
{
    /// <summary>
    /// Gets the <c>MessageBoxResult</c> name.
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; init; }
}
