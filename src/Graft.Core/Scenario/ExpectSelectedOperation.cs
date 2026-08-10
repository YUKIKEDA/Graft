namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>selected</c> state.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Selected">Expected selection state.</param>
public sealed record ExpectSelectedOperation(string AutomationId, bool Selected)
    : ScenarioOperation(ScenarioActions.ExpectSelected);
