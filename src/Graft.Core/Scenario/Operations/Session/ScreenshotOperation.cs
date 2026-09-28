using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: capture a window or element screenshot to a file path.
/// </summary>
/// <param name="Path">Destination PNG path.</param>
/// <param name="Target">Optional element to clip; window when omitted.</param>
public sealed record ScreenshotOperation(string Path, Selector? Target = null) : ScenarioOperation(ScenarioActions.Screenshot);
