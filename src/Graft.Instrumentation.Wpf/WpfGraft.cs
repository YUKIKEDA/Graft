using Graft.Instrumentation.Tree;

namespace Graft.Instrumentation.Wpf;

/// <summary>
/// Registration entry point for the WPF instrumentation adapter.
/// </summary>
public static class WpfGraft
{
    private static int _registered;

    /// <summary>
    /// Registers WPF providers for <c>getTree</c>, <c>screenshot</c>, and element resolve.
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

        AgentServices.RegisterTreeProvider(new WpfUiTreeProvider());
        AgentServices.RegisterScreenshotProvider(new WpfScreenshotProvider());
        AgentServices.RegisterElementResolver(new WpfElementResolver());
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
