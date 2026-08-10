namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>expanded</c> state.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Expanded">Expected expand state.</param>
public sealed record ExpectExpandedOperation(string AutomationId, bool Expanded)
    : ScenarioOperation(ScenarioActions.ExpectExpanded);
