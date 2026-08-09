using Graft.Core;

namespace SampleWpfApp.Tests;

/// <summary>
/// Example consumer E2E tests for <c>SampleWpfApp</c>.
/// </summary>
/// <remarks>
/// Role split (this is the usage pattern for product apps):
/// <list type="bullet">
/// <item>
/// <description>
/// App under test (<c>SampleWpfApp</c>): references <c>Graft.Instrumentation.Wpf</c>,
/// calls <c>WpfGraft.Use()</c> / <c>Agent.Start()</c> under <c>GRAFT_TEST</c>.
/// </description>
/// </item>
/// <item>
/// <description>
/// Test project (this assembly): references <c>Graft.Core</c> only, launches the app,
/// then drives UI with <c>GetByAutomationId</c> /
/// <c>InvokeAsync</c> / <c>SetValueAsync</c> / <c>ExpectNameAsync</c>.
/// </description>
/// </item>
/// </list>
/// </remarks>
public sealed class MainWindowE2ETests
{
    /// <summary>
    /// Clicking SampleButton updates StatusText to Clicked 1.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    ///
    /// Steps:
    /// - Application.LaunchAsync(sample csproj) — sets GRAFT_ENABLE / PIPE / TOKEN
    /// - GetByAutomationId("SampleButton").InvokeAsync()
    /// - GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1")
    ///
    /// Expected:
    /// - Expectation passes; disposing the session stops the app process
    /// </remarks>
    [Fact]
    public async Task ClickSampleButton_UpdatesStatusText()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleButton").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("Clicked 1");
    }

    /// <summary>
    /// setValue replaces SampleTextBox text and ExpectName sees the new value.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    ///
    /// Steps:
    /// - Launch sample
    /// - GetByAutomationId("SampleTextBox").SetValueAsync("hello-graft")
    /// - ExpectNameAsync("hello-graft")
    ///
    /// Expected:
    /// - TextBox name in the tree equals the set value
    /// </remarks>
    [Fact]
    public async Task SetValue_SampleTextBox_UpdatesName()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        const string typed = "hello-graft";
        await app.GetByAutomationId("SampleTextBox").SetValueAsync(typed);
        await app.GetByAutomationId("SampleTextBox").ExpectNameAsync(typed);
    }

    /// <summary>
    /// toggle flips SampleCheckBox Content from Off to On.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    ///
    /// Steps:
    /// - Launch sample
    /// - GetByAutomationId("SampleCheckBox").ToggleAsync()
    /// - ExpectNameAsync("On")
    ///
    /// Expected:
    /// - CheckBox tree name is On
    /// </remarks>
    [Fact]
    public async Task Toggle_SampleCheckBox_UpdatesName()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleCheckBox").ToggleAsync();
        await app.GetByAutomationId("SampleCheckBox").ExpectNameAsync("On");
    }

    /// <summary>
    /// sendKeys appends text into SampleTextBox.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    ///
    /// Steps:
    /// - Launch sample
    /// - GetByAutomationId("SampleTextBox").SendKeysAsync("keys-graft")
    /// - ExpectNameAsync("keys-graft")
    ///
    /// Expected:
    /// - TextBox name equals the typed value
    /// </remarks>
    [Fact]
    public async Task SendKeys_SampleTextBox_UpdatesName()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        const string typed = "keys-graft";
        await app.GetByAutomationId("SampleTextBox").SendKeysAsync(typed);
        await app.GetByAutomationId("SampleTextBox").ExpectNameAsync(typed);
    }

    /// <summary>
    /// invoke on SampleMouseTarget uses SendInput fallback and updates StatusText.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Sibling SampleWpfApp.csproj can build with Configuration=GraftTest
    ///
    /// Steps:
    /// - Launch sample
    /// - GetByAutomationId("SampleMouseTarget").InvokeAsync()
    /// - ExpectNameAsync("MouseHit") on StatusText
    ///
    /// Expected:
    /// - StatusText name is MouseHit
    /// </remarks>
    [Fact]
    public async Task Invoke_SampleMouseTarget_ViaSendInput_UpdatesStatus()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("SampleMouseTarget").InvokeAsync();
        await app.GetByAutomationId("StatusText").ExpectNameAsync("MouseHit");
    }
}
