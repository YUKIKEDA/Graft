using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Tree;

#if GRAFT_TEST

/// <summary>
/// Framework-specific UI tree capture used by the agent pipe server.
/// </summary>
public interface IUiTreeProvider
{
    /// <summary>
    /// Captures the visual tree, marshaling to the UI thread as required.
    /// </summary>
    /// <param name="options">Depth / node limits.</param>
    /// <returns>Tree root and truncation flag.</returns>
    GetTreeResult GetTree(GetTreeOptions options);
}

#endif
