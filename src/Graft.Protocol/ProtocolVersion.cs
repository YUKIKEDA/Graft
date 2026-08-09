namespace Graft.Protocol;

/// <summary>
/// Wire protocol version. Handshake requires an exact match (project.md Q47).
/// </summary>
public static class ProtocolVersion
{
    /// <summary>
    /// Current protocol version accepted by agents and controllers.
    /// </summary>
    public const int Current = 1;
}
