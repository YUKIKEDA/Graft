using Graft.Protocol.Messages;

namespace Graft.Instrumentation.Windows;

#if GRAFT_TEST

/// <summary>
/// Lists and selects the agent target window.
/// </summary>
public interface IWindowCatalog
{
    /// <summary>
    /// Lists open windows with session-local ids.
    /// </summary>
    /// <returns>Window list result.</returns>
    ListWindowsResult ListWindows();

    /// <summary>
    /// Sets the target window for subsequent getTree / resolve / screenshot / actions.
    /// </summary>
    /// <param name="windowId">Session-local window id from <see cref="ListWindows"/>.</param>
    void SwitchWindow(int windowId);
}

#endif
