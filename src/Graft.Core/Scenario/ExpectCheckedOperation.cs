namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>checked</c> state.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Checked">Expected checked state.</param>
public sealed record ExpectCheckedOperation(string AutomationId, bool Checked)
    : ScenarioOperation(ScenarioActions.ExpectChecked);
