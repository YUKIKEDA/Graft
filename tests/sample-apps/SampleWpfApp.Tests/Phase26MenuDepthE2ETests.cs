using Graft.Core;
using Graft.Protocol;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 26 menu depth / SelectMenuAsync acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase26MenuDepthE2ETests
{
    /// <summary>
    /// SelectMenu deep path on Menu bar updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleMenu exposes File/Recent/OpenRecent path
    ///
    /// Steps:
    /// - Launch sample
    /// - SelectMenuAsync("SampleMenuFile/SampleMenuRecent/SampleMenuOpenRecent") on SampleMenu
    /// - ExpectNameAsync("MenuOpenRecent") on StatusText
    ///
    /// Expected:
    /// - StatusText is MenuOpenRecent
    /// </remarks>
    [Fact]
    public async Task SelectMenu_MenuBarDeepPath_UpdatesStatusText()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleMenu").SelectMenuAsync("SampleMenuFile/SampleMenuRecent/SampleMenuOpenRecent");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("MenuOpenRecent");
    }

    /// <summary>
    /// RightClick then SelectMenu on ContextMenu submenu updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - ContextMenuTarget has SampleContextMenu with More/SubPing
    ///
    /// Steps:
    /// - RightClickAsync on ContextMenuTarget
    /// - SelectMenuAsync("ContextMenuMore/ContextMenuSubPing") on SampleContextMenu
    /// - ExpectNameAsync("ContextMenuSubPing") on StatusText
    ///
    /// Expected:
    /// - StatusText is ContextMenuSubPing
    /// </remarks>
    [Fact]
    public async Task SelectMenu_ContextMenuSubPath_UpdatesStatusText()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("ContextMenuTarget").RightClickAsync();
        await app.GetByAutomationId("SampleContextMenu").SelectMenuAsync("ContextMenuMore/ContextMenuSubPing");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("ContextMenuSubPing");
    }

    /// <summary>
    /// Selecting a disabled menu item fails with element.notActionable.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleMenuFile/SampleMenuDisabled exists and IsEnabled=False
    ///
    /// Steps:
    /// - SelectMenuAsync disabled path on SampleMenu
    ///
    /// Expected:
    /// - GraftException with Code element.notActionable
    /// </remarks>
    [Fact]
    public async Task SelectMenu_DisabledItem_ThrowsElementNotActionable()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var ex = await Assert.ThrowsAsync<GraftException>(() =>
            app.GetByAutomationId("SampleMenu").SelectMenuAsync("SampleMenuFile/SampleMenuDisabled")
        );
        Assert.Equal(GraftErrorCodes.ElementNotActionable, ex.Code);
    }
}
