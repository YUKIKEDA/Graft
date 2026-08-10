using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 29b list / misc control acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase29bControlsE2ETests
{
    /// <summary>
    /// DatePicker, ComboBox expand, ListView cell read, ToolTip, ToolBar, Popup, Hyperlink.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample Phase29b controls exist on MainWindow
    ///
    /// Steps:
    /// - SetValue/ExpectValue DatePicker yyyy-MM-dd
    /// - Expand/ExpectExpanded/Collapse ComboBox
    /// - Select ListView row + ExpectCellText by Header
    /// - Hover TipHost → ExpectToolTip
    /// - Click ToolBar → Expect StatusBar name
    /// - Open Popup → Click inner button → StatusText
    /// - Click Hyperlink → StatusText
    ///
    /// Expected:
    /// - All Phase 29b APIs update Status / tree state as specified
    /// </remarks>
    [Fact]
    public async Task Phase29b_ListAndMiscControls_Work()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SamplePhase29bDate").SetValueAsync("2026-08-11");
        await app.GetByAutomationId("SamplePhase29bDate").ExpectValueAsync("2026-08-11");

        await app.GetByAutomationId("SamplePhase29bCombo").ExpandAsync();
        await app.GetByAutomationId("SamplePhase29bCombo").ExpectExpandedAsync(true);
        await app.GetByAutomationId("SamplePhase29bCombo").CollapseAsync();
        await app.GetByAutomationId("SamplePhase29bCombo").ExpectExpandedAsync(false);

        await app.GetByAutomationId("SamplePhase29bListView").SelectAsync(1);
        await app.GetByAutomationId("SamplePhase29bListView").ExpectCellTextAsync(1, "Name", "Bob");
        await app.GetByAutomationId("SamplePhase29bListView").ExpectCellTextAsync(1, "Notes", "B2");

        await app.GetByAutomationId("SamplePhase29bTipHost").ScrollIntoViewAsync();
        await app.GetByAutomationId("SamplePhase29bTipHost").HoverAsync();
        await app.GetByAutomationId("SamplePhase29bTipHost").ExpectToolTipAsync("Phase29bTip");

        await app.GetByAutomationId("SamplePhase29bToolBarButton").InvokeAsync();
        await app.GetByAutomationId("SamplePhase29bStatusBarText").ExpectNameAsync("TB-clicked");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Phase29bToolBar");

        await app.GetByAutomationId("SamplePhase29bOpenPopup").InvokeAsync();
        await app.GetByAutomationId("SamplePhase29bPopupButton").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Phase29bPopupBtn");

        await app.GetByAutomationId("SamplePhase29bHyperlink").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Phase29bHyperlink");
    }
}
