using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Phase 29a controls / keys acceptance for SampleWpfApp.
/// </summary>
[Collection(SampleUiCollection.Name)]
public sealed class Phase29aControlsE2ETests
{
    /// <summary>
    /// Password set, RichText plain, Radio/Toggle checked, Tab focus, F5 chord.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sample Phase29a controls exist on MainWindow
    ///
    /// Steps:
    /// - SetValue Password → StatusText length
    /// - SetValue RichText → ExpectValue
    /// - Toggle RadioB → ExpectChecked; Toggle Toggle → ExpectChecked
    /// - Press Tab from FocusA → ExpectFocused on FocusB
    /// - Press F5 → StatusText Phase29aKey F5
    ///
    /// Expected:
    /// - All Phase 29a APIs update Status / tree state as specified
    /// </remarks>
    [Fact]
    public async Task Phase29a_PasswordRichToggleFocusKeys_Works()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SamplePhase29aPassword").SetValueAsync("secret");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Phase29aPassword len=6");

        await app.GetByAutomationId("SamplePhase29aRichText").SetValueAsync("HelloRich");
        await app.GetByAutomationId("SamplePhase29aRichText").ExpectValueAsync("HelloRich");

        await app.GetByAutomationId("SamplePhase29aRadioB").ToggleAsync();
        await app.GetByAutomationId("SamplePhase29aRadioB").ExpectCheckedAsync(true);
        await app.GetByAutomationId("SamplePhase29aRadioA").ExpectCheckedAsync(false);

        await app.GetByAutomationId("SamplePhase29aToggle").ToggleAsync();
        await app.GetByAutomationId("SamplePhase29aToggle").ExpectCheckedAsync(true);

        await app.GetByAutomationId("SamplePhase29aFocusA").PressAsync("Tab");
        await app.GetByAutomationId("SamplePhase29aFocusB").ExpectFocusedAsync();

        await app.GetByAutomationId("SamplePhase29aFocusB").PressAsync("F5");
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Phase29aKey F5");
    }
}
