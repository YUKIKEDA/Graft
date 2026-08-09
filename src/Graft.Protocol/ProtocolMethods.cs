namespace Graft.Protocol;

/// <summary>
/// Wire <c>method</c> names used in request envelopes (camelCase).
/// </summary>
public static class ProtocolMethods
{
    /// <summary>
    /// Establishes a session: protocol version must match and <c>params.token</c> must equal
    /// <c>GRAFT_CONNECT_TOKEN</c>.
    /// </summary>
    public const string Handshake = "handshake";
}
