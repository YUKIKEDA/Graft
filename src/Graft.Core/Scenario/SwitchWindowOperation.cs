namespace Graft.Core.Scenario;

/// <summary>
/// Switches the agent target window by session-local id.
/// </summary>
/// <param name="WindowId">Session-local window id.</param>
public sealed record SwitchWindowOperation(int WindowId)
    : ScenarioOperation(ScenarioActions.SwitchWindow);
