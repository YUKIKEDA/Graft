namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>enabled</c> state.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Enabled">Expected enabled state.</param>
public sealed record ExpectEnabledOperation(string AutomationId, bool Enabled)
    : ScenarioOperation(ScenarioActions.ExpectEnabled);
