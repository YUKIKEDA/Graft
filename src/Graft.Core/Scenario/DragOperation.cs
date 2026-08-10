namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: drag from one element to another (SendInput).
/// </summary>
/// <param name="AutomationId">Source automation id.</param>
/// <param name="ToAutomationId">Target automation id.</param>
public sealed record DragOperation(string AutomationId, string ToAutomationId)
    : ScenarioOperation(ScenarioActions.Drag);
