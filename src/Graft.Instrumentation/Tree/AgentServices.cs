using Graft.Instrumentation.Actions;
using Graft.Instrumentation.Elements;
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
    private static IElementResolver? _elementResolver;
    private static IElementInvoker? _elementInvoker;

    /// <summary>
    /// Gets the registered UI tree provider, if any.
    /// </summary>
    public static IUiTreeProvider? TreeProvider => _treeProvider;

    /// <summary>
    /// Gets the registered screenshot provider, if any.
    /// </summary>
    public static IScreenshotProvider? ScreenshotProvider => _screenshotProvider;

    /// <summary>
    /// Gets the registered element resolver, if any.
    /// </summary>
    public static IElementResolver? ElementResolver => _elementResolver;

    /// <summary>
    /// Gets the registered element invoker, if any.
    /// </summary>
    public static IElementInvoker? ElementInvoker => _elementInvoker;

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
    /// Registers the element resolver used for invoke / setValue.
    /// </summary>
    /// <param name="resolver">Framework-specific resolver.</param>
    public static void RegisterElementResolver(IElementResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        _elementResolver = resolver;
    }

    /// <summary>
    /// Registers the element invoker used for <c>invoke</c>.
    /// </summary>
    /// <param name="invoker">Framework-specific invoker.</param>
    public static void RegisterElementInvoker(IElementInvoker invoker)
    {
        ArgumentNullException.ThrowIfNull(invoker);
        _elementInvoker = invoker;
    }

    /// <summary>
    /// Clears registered services (tests).
    /// </summary>
    public static void Reset()
    {
        _treeProvider = null;
        _screenshotProvider = null;
        _elementResolver = null;
        _elementInvoker = null;
    }
}

#endif
