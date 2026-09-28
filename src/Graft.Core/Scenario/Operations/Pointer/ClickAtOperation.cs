using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: left-click at clickable point plus DIP offsets (SendInput).
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="OffsetX">Horizontal DIP offset.</param>
/// <param name="OffsetY">Vertical DIP offset.</param>
public sealed record ClickAtOperation(Selector Target, double OffsetX, double OffsetY) : ScenarioOperation(ScenarioActions.ClickAt);
