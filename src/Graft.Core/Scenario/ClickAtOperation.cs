namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: left-click at clickable point plus DIP offsets (SendInput).
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="OffsetX">Horizontal DIP offset.</param>
/// <param name="OffsetY">Vertical DIP offset.</param>
public sealed record ClickAtOperation(string AutomationId, double OffsetX, double OffsetY) : ScenarioOperation(ScenarioActions.ClickAt);
