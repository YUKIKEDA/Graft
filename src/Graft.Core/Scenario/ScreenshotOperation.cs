namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: capture the target window screenshot to a file path.
/// </summary>
/// <param name="Path">Destination PNG path.</param>
public sealed record ScreenshotOperation(string Path)
    : ScenarioOperation(ScenarioActions.Screenshot);
