namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: select a TreeView path under a TreeView root.
/// </summary>
/// <param name="AutomationId">TreeView automation id.</param>
/// <param name="Path">Slash-separated AutomationId segments (root not included).</param>
public sealed record SelectTreeOperation(string AutomationId, string Path) : ScenarioOperation(ScenarioActions.SelectTree);
