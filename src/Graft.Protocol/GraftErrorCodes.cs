namespace Graft.Protocol;

/// <summary>
/// Stable wire / Core error codes (project.md).
/// </summary>
public static class GraftErrorCodes
{
    public const string HandshakeRejected = "handshake.rejected";
    public const string ProtocolVersionMismatch = "protocol.versionMismatch";
    public const string ElementNotFound = "element.notFound";
    public const string ElementAmbiguous = "element.ambiguous";
    public const string ElementNotActionable = "element.notActionable";
    public const string ActionTimeout = "action.timeout";
    public const string ActionFailed = "action.failed";
    public const string WindowNotFound = "window.notFound";
    public const string PipeDisconnected = "pipe.disconnected";
    public const string AgentNotEnabled = "agent.notEnabled";
    public const string ExpectFailed = "expect.failed";
    public const string SelectorInvalid = "selector.invalid";
}
