using Graft.Instrumentation.Tree;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Registration entry point for the WPF instrumentation adapter.
/// </summary>
public static class WpfGraft
{
    private static int _registered;

    /// <summary>
    /// Registers WPF providers for tree, screenshot, resolve, windows, and element actions.
    /// </summary>
    /// <remarks>
    /// Call once before <see cref="Agent.Start"/> (typically from <c>OnStartup</c>).
    /// </remarks>
    public static void Use()
    {
        if (Interlocked.Exchange(ref _registered, 1) != 0)
        {
            return;
        }

        var windows = new WpfWindowHost();
        AgentServices.RegisterWindowCatalog(windows);
        AgentServices.RegisterTreeProvider(new WpfUiTreeProvider(windows));
        AgentServices.RegisterScreenshotProvider(new WpfScreenshotProvider(windows));
        AgentServices.RegisterElementResolver(new WpfElementResolver(windows));
        AgentServices.RegisterElementInvoker(new WpfElementInvoker());
        AgentServices.RegisterElementValueSetter(new WpfElementValueSetter());
        AgentServices.RegisterElementToggler(new WpfElementToggler());
        AgentServices.RegisterElementKeySender(new WpfElementKeySender());
        AgentServices.RegisterElementScroller(new WpfElementScroller());
        AgentServices.RegisterElementChooser(new WpfElementChooser());
        AgentServices.RegisterElementExpander(new WpfElementExpander());
    }

    /// <summary>
    /// Clears registration state (tests).
    /// </summary>
    public static void ResetForTests()
    {
        _registered = 0;
        AgentServices.Reset();
    }
}
