using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase7WindowsE2ETests
{
    /// <summary>
    /// Opens a modeless child window, lists/waits/switches, then Expects on the child.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp OpenChildWindowButton opens ChildWindow via Show()
    ///
    /// Steps:
    /// - Invoke OpenChildWindowButton
    /// - ListWindowsAsync includes ChildWindow
    /// - WaitForWindowAsync(automationId: ChildWindow) switches target
    /// - ExpectNameAsync ChildReady on ChildStatus
    ///
    /// Expected:
    /// - Child window is operable after switch
    /// </remarks>
    [Fact]
    public async Task ModelessChild_ListWaitSwitch_ThenExpectOnChild()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("OpenChildWindowButton").InvokeAsync();

        var listed = await app.ListWindowsAsync();
        Assert.Contains(listed.Windows, w => w.AutomationId == "ChildWindow");

        var child = await app.WaitForWindowAsync(automationId: "ChildWindow");
        Assert.Equal("ChildWindow", child.AutomationId);

        await app.GetByAutomationId("ChildStatus").ExpectNameAsync("ChildReady");
    }

    /// <summary>
    /// Opens a modal via InvokeOpeningWindow, operates on it, then closes.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp OpenModalWindowButton opens ModalWindow via ShowDialog()
    ///
    /// Steps:
    /// - InvokeOpeningWindowAsync on OpenModalWindowButton
    /// - ExpectNameAsync ModalReady on ModalStatus
    /// - Invoke CloseModalButton
    /// - WaitForWindowAsync Main and Expect StatusText ModalClosed
    ///
    /// Expected:
    /// - Modal is operable through the dedicated open path (plain Invoke would hang)
    /// </remarks>
    [Fact]
    public async Task Modal_InvokeOpeningWindow_ThenOperateAndClose()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        var modal = await app.GetByAutomationId("OpenModalWindowButton").InvokeOpeningWindowAsync();
        Assert.True(modal.IsModal);
        Assert.Equal("ModalWindow", modal.AutomationId);

        await app.GetByAutomationId("ModalStatus").ExpectNameAsync("ModalReady");
        await app.GetByAutomationId("CloseModalButton").InvokeAsync();

        await app.WaitForWindowAsync(automationId: "Main");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("ModalClosed");
    }
}
