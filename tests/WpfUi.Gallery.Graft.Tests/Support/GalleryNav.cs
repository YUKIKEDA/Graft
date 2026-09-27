using Graft.Core;

namespace WpfUi.Gallery.Graft.Tests;

/// <summary>
/// Shared navigation and wait helpers for Gallery E2E.
/// </summary>
internal static class GalleryNav
{
    /// <summary>
    /// Applies default Action/Expect timeouts for Gallery pages.
    /// </summary>
    /// <param name="session">Active Graft session.</param>
    public static void ApplyDefaultWaits(GraftSession session)
    {
        session.WaitOptions = new WaitOptions { ActionTimeout = TimeSpan.FromSeconds(30), ExpectTimeout = TimeSpan.FromSeconds(30) };
    }

    /// <summary>
    /// Ensures the category fixture launched Gallery; throws a clear message otherwise.
    /// </summary>
    /// <param name="fixture">Category fixture.</param>
    public static void EnsureReady(GalleryCategoryFixture fixture)
    {
        if (!fixture.IsReady)
        {
            throw new InvalidOperationException("Gallery was not launched. Ensure tests/wpfui is cloned and Graft-patched.");
        }
    }

    /// <summary>
    /// Double-clicks a leaf NavigationViewItem and settles briefly.
    /// </summary>
    /// <param name="session">Active session.</param>
    /// <param name="navAutomationId">Nav_* AutomationId.</param>
    public static async Task NavigateLeafAsync(GraftSession session, string navAutomationId)
    {
        var leaf = session.GetByAutomationId(navAutomationId);
        await leaf.ScrollIntoViewAsync();
        await leaf.DoubleClickAsync();
        await Task.Delay(400);
    }

    /// <summary>
    /// Expands a parent nav item then navigates to a leaf.
    /// WPF UI <c>NavigationViewItem</c> supports Expand via AutomationPeer, but Graft tree
    /// reports <c>expanded=n/a</c> — wait on leaf visibility instead of ExpectExpanded.
    /// </summary>
    /// <param name="session">Active session.</param>
    /// <param name="parentNavId">Parent Nav_* id.</param>
    /// <param name="leafNavId">Leaf Nav_* id.</param>
    public static async Task NavigateAsync(GraftSession session, string parentNavId, string leafNavId)
    {
        await session.GetByAutomationId(parentNavId).ExpandAsync();
        await Task.Delay(200);

        var leaf = session.GetByAutomationId(leafNavId);
        await leaf.ExpectVisibleAsync(true);
        await leaf.ScrollIntoViewAsync();
        await leaf.DoubleClickAsync();
        await Task.Delay(400);
    }

    /// <summary>
    /// Best-effort dismiss for Gallery ContentDialog (AutomationId buttons + name fallback).
    /// Waits for a dialog button — Show is async and can race.
    /// </summary>
    /// <param name="session">Active session.</param>
    public static async Task DismissContentDialogBestEffortAsync(GraftSession session)
    {
        foreach (var id in new[] { "ContentDialog_CloseButton", "ContentDialog_PrimaryButton", "ContentDialog_SecondaryButton" })
        {
            try
            {
                var btn = session.GetByAutomationId(id);
                await btn.ExpectVisibleAsync(true);
                await btn.InvokeAsync();
                await Task.Delay(400);
                return;
            }
            catch (GraftException)
            {
                // try next
            }
        }

        foreach (var name in new[] { "Cancel", "Save", "Close", "OK" })
        {
            try
            {
                var btn = session.GetByName(name);
                await btn.ExpectVisibleAsync(true);
                await btn.InvokeAsync();
                await Task.Delay(400);
                return;
            }
            catch (GraftException)
            {
                // try next label
            }
        }
    }

    /// <summary>
    /// Scrolls an element into view then returns the query for chaining actions.
    /// </summary>
    /// <param name="session">Active session.</param>
    /// <param name="automationId">Target AutomationId.</param>
    /// <returns>Element query after ScrollIntoView.</returns>
    public static async Task<ElementQuery> BringAsync(GraftSession session, string automationId)
    {
        var el = session.GetByAutomationId(automationId);
        try
        {
            await el.ScrollIntoViewAsync();
        }
        catch (GraftException)
        {
            // Element may already be in view, or ScrollIntoView waits on actionable (off-screen chicken/egg).
        }

        await el.ExpectVisibleAsync(true);
        return el;
    }

    /// <summary>
    /// Captures a window screenshot and asserts non-empty PNG.
    /// </summary>
    /// <param name="session">Active session.</param>
    public static async Task ShotAsync(GraftSession session)
    {
        var shot = await session.ScreenshotAsync();
        Assert.NotNull(shot.PngBytes);
        Assert.NotEmpty(shot.PngBytes);
        Assert.True(shot.Width > 0);
        Assert.True(shot.Height > 0);
    }
}
