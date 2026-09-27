using Graft.Instrumentation;
using Graft.Instrumentation.Wpf.Dialogs;

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
    /// Also installs CommonItemDialog and MessageBox seams (Harmony) once per process.
    /// The patches are intentionally never removed (there is no <c>Unuse()</c>, and
    /// <see cref="Agent.Stop"/> does not unpatch): the agent targets one test process per app
    /// launch, and an un-armed seam falls through to the original dialog.
    /// </remarks>
    public static void Use()
    {
        CommonItemDialogPatch.Apply();
        MessageBoxPatch.Apply();

        if (Interlocked.Exchange(ref _registered, 1) != 0)
        {
            return;
        }

        var windows = new WpfWindowHost();
        var resolver = new WpfElementResolver(windows);
        Agent.Use(
            new AgentBackend
            {
                WindowCatalog = windows,
                TreeProvider = new WpfUiTreeProvider(windows),
                ScreenshotProvider = new WpfScreenshotProvider(windows),
                ElementResolver = resolver,
                ElementInvoker = new WpfElementInvoker(resolver),
                ElementValueSetter = new WpfElementValueSetter(resolver),
                ElementToggler = new WpfElementToggler(resolver),
                ElementKeySender = new WpfElementKeySender(resolver),
                ElementScroller = new WpfElementScroller(resolver),
                ElementChooser = new WpfElementChooser(resolver),
                MenuSelector = new WpfMenuSelector(resolver),
                TreeSelector = new WpfTreeSelector(resolver),
                ElementExpander = new WpfElementExpander(resolver),
                ElementCellAccessor = new WpfDataGridCellAccessor(resolver),
                DataGridOperator = new WpfDataGridOperator(resolver),
            }
        );
    }

    /// <summary>
    /// Clears registration state (tests).
    /// </summary>
    public static void ResetForTests()
    {
        _registered = 0;
        Agent.Reset();
        WpfControlActions.Clear();
    }
}
