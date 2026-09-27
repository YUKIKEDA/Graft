using System.Text.Json.Serialization;

namespace Graft.Protocol.Messages;

/// <summary>
/// Params for <c>handshake</c>.
/// </summary>
public sealed class HandshakeParams
{
    /// <summary>
    /// Gets the connect token.
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; init; }
}
