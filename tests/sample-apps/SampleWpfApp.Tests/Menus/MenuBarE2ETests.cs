using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 20 Menu bar invoke acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class MenuBarE2ETests
{
    /// <summary>
    /// Invoke File then Ping updates StatusText via Menu bar submenu.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    /// - SampleMenuFile opens SampleMenuPing
    ///
    /// Steps:
    /// - Launch sample
    /// - InvokeAsync on SampleMenuFile
    /// - InvokeAsync on SampleMenuPing
    /// - ExpectNameAsync("MenuPing") on StatusText
    ///
    /// Expected:
    /// - StatusText is MenuPing
    /// </remarks>
    [Fact]
    public async Task Invoke_MenuFilePing_UpdatesStatusText()
    {
        await using var app = await SampleWpfLaunch.LaunchAsync();

        await app.GetByAutomationId("SampleMenuFile").InvokeAsync();
        await app.GetByAutomationId("SampleMenuPing").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("MenuPing");
    }
}
