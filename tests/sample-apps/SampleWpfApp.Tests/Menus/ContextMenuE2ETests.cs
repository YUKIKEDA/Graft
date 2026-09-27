using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 16 ContextMenu right-click acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class ContextMenuE2ETests
{
    /// <summary>
    /// Right-click opens ContextMenu; invoke MenuItem updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    /// - ContextMenuTarget has ContextMenuPing MenuItem
    ///
    /// Steps:
    /// - Launch sample
    /// - RightClickAsync on ContextMenuTarget
    /// - InvokeAsync on ContextMenuPing
    /// - ExpectNameAsync("ContextMenuPing") on StatusText
    ///
    /// Expected:
    /// - StatusText name is ContextMenuPing
    /// </remarks>
    [Fact]
    public async Task RightClick_ContextMenuPing_UpdatesStatusText()
    {
        await using var app = await SampleWpfLaunch.LaunchAsync();

        await app.GetByAutomationId("ContextMenuTarget").RightClickAsync();
        await app.GetByAutomationId("ContextMenuPing").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("ContextMenuPing");
    }
}
