namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: capture a window or element screenshot to a file path.
/// </summary>
/// <param name="Path">Destination PNG path.</param>
/// <param name="AutomationId">Optional element to clip; window when omitted.</param>
public sealed record ScreenshotOperation(string Path, string? AutomationId = null) : ScenarioOperation(ScenarioActions.Screenshot);
