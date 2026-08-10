namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>visible</c> state.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Visible">Expected visible state.</param>
public sealed record ExpectVisibleOperation(string AutomationId, bool Visible)
    : ScenarioOperation(ScenarioActions.ExpectVisible);
