using Graft.Core;

namespace SampleWpfApp.Tests;

[Collection(SampleUiCollection.Name)]
public sealed class Phase25MouseE2ETests
{
    /// <summary>
    /// Exercises double-click, hover, drag, clickAt, and wheel on the Sample Mouse section.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SampleWpfApp Mouse advanced targets update StatusText / Automation Name
    ///
    /// Steps:
    /// - DoubleClickAsync DoubleClickTarget → Expect DoubleClicked
    /// - HoverAsync HoverTarget → Expect Hovered
    /// - DragAsync DragSource → DropTarget → Expect Dropped
    /// - ClickAtAsync ClickAtPad (-40,0) → Expect ClickAtLeft
    /// - WheelAsync WheelScroller (-360) → Expect WheelScrolled
    ///
    /// Expected:
    /// - Each SendInput mouse path updates StatusText
    /// </remarks>
    [Fact]
    public async Task MouseAdvanced_DoubleHoverDragClickAtWheel()
    {
        await using var app = await Application.LaunchAsync(
            new LaunchOptions
            {
                AppPath = SampleAppLocator.ResolveProjectPath(),
                Configuration = "GraftTest",
                Timeout = TimeSpan.FromSeconds(60),
            }
        );

        await app.GetByAutomationId("DoubleClickTarget").ScrollIntoViewAsync();
        await app.GetByAutomationId("DoubleClickTarget").DoubleClickAsync();
        await app.GetByAutomationId("DoubleClickTarget").ExpectNameAsync("DoubleClicked");

        await app.GetByAutomationId("HoverTarget").HoverAsync();
        await app.GetByAutomationId("HoverTarget").ExpectNameAsync("Hovered");

        await app.GetByAutomationId("DragSource").DragAsync("DropTarget");
        await app.GetByAutomationId("DropTarget").ExpectNameAsync("Dropped");

        await app.GetByAutomationId("ClickAtPad").ScrollIntoViewAsync();
        await app.GetByAutomationId("ClickAtPad").ClickAtAsync(-40, 0);
        await app.GetByAutomationId("ClickAtPad").ExpectNameAsync("ClickAtLeft");

        await app.GetByAutomationId("WheelScroller").ScrollIntoViewAsync();
        await app.GetByAutomationId("WheelScroller").WheelAsync(-360);
        await app.GetByAutomationId("WheelBottomLabel").ExpectNameAsync("WheelScrolled");
    }
}
