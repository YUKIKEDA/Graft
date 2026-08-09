namespace Graft.Instrumentation.Tree;

#if GRAFT_TEST

/// <summary>
/// Process-wide agent service registration (framework adapters).
/// </summary>
public static class AgentServices
{
    private static IUiTreeProvider? _treeProvider;

    /// <summary>
    /// Gets the registered UI tree provider, if any.
    /// </summary>
    public static IUiTreeProvider? TreeProvider => _treeProvider;

    /// <summary>
    /// Registers the UI tree provider used for <c>getTree</c>.
    /// </summary>
    /// <param name="provider">Framework-specific provider.</param>
    public static void RegisterTreeProvider(IUiTreeProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _treeProvider = provider;
    }

    /// <summary>
    /// Clears registered services (tests).
    /// </summary>
    public static void Reset()
    {
        _treeProvider = null;
    }
}

#endif
