namespace Graft.Protocol;

/// <summary>
/// Stable wire / Core error codes (project.md).
/// </summary>
public static class GraftErrorCodes
{
    /// <summary>
    /// Handshake failed (token or other rejection).
    /// </summary>
    public const string HandshakeRejected = "handshake.rejected";

    /// <summary>
    /// Client and agent protocol versions do not match.
    /// </summary>
    public const string ProtocolVersionMismatch = "protocol.versionMismatch";

    /// <summary>
    /// No element matched the selector.
    /// </summary>
    public const string ElementNotFound = "element.notFound";

    /// <summary>
    /// Multiple elements tied for the best selector score.
    /// </summary>
    public const string ElementAmbiguous = "element.ambiguous";

    /// <summary>
    /// Element exists but is not actionable (e.g. not visible or enabled).
    /// </summary>
    public const string ElementNotActionable = "element.notActionable";

    /// <summary>
    /// An action or wait timed out.
    /// </summary>
    public const string ActionTimeout = "action.timeout";

    /// <summary>
    /// An action failed for a non-timeout reason.
    /// </summary>
    public const string ActionFailed = "action.failed";

    /// <summary>
    /// Target window was not found.
    /// </summary>
    public const string WindowNotFound = "window.notFound";

    /// <summary>
    /// The named pipe connection was lost.
    /// </summary>
    public const string PipeDisconnected = "pipe.disconnected";

    /// <summary>
    /// Agent is not enabled (e.g. <c>GRAFT_ENABLE</c> missing).
    /// </summary>
    public const string AgentNotEnabled = "agent.notEnabled";

    /// <summary>
    /// An expectation / assert step failed (Core-side).
    /// </summary>
    public const string ExpectFailed = "expect.failed";

    /// <summary>
    /// Selector syntax or structure is invalid.
    /// </summary>
    public const string SelectorInvalid = "selector.invalid";
}
