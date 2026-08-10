namespace Graft.Core.Scenario;

/// <summary>
/// Invokes an element that opens a window (modal-safe path) and switches to the new window.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record InvokeOpeningWindowOperation(string AutomationId)
    : ScenarioOperation(ScenarioActions.InvokeOpeningWindow);
