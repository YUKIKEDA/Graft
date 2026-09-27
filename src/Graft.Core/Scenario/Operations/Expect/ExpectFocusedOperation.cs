namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>focused</c> state to be true.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
public sealed record ExpectFocusedOperation(string AutomationId) : ScenarioOperation(ScenarioActions.ExpectFocused);
