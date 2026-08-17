namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>name</c>.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="Name">Expected name.</param>
public sealed record ExpectNameOperation(string AutomationId, string Name) : ScenarioOperation(ScenarioActions.ExpectName);
