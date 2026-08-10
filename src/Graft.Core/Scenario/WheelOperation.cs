namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: scroll the mouse wheel over an element (SendInput).
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Delta">Wheel delta (typically multiples of 120).</param>
public sealed record WheelOperation(string AutomationId, int Delta)
    : ScenarioOperation(ScenarioActions.Wheel);
