using Graft.Instrumentation.Screenshot;

namespace Graft.Instrumentation.Tree;

#if GRAFT_TEST

/// <summary>
/// Process-wide agent service registration (framework adapters).
/// </summary>
public static class AgentServices
{
    private static IUiTreeProvider? _treeProvider;
    private static IScreenshotProvider? _screenshotProvider;

    /// <summary>
    /// Gets the registered UI tree provider, if any.
    /// </summary>
    public static IUiTreeProvider? TreeProvider => _treeProvider;

    /// <summary>
    /// Gets the registered screenshot provider, if any.
    /// </summary>
    public static IScreenshotProvider? ScreenshotProvider => _screenshotProvider;

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
    /// Registers the screenshot provider used for <c>screenshot</c>.
    /// </summary>
    /// <param name="provider">Framework-specific provider.</param>
    public static void RegisterScreenshotProvider(IScreenshotProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _screenshotProvider = provider;
    }

    /// <summary>
    /// Clears registered services (tests).
    /// </summary>
    public static void Reset()
    {
        _treeProvider = null;
        _screenshotProvider = null;
    }
}

#endif
