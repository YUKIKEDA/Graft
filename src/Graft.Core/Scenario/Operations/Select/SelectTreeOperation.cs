using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: select a TreeView path under a TreeView root.
/// </summary>
/// <param name="Target">TreeView automation id.</param>
/// <param name="Path">Slash-separated AutomationId segments (root not included).</param>
public sealed record SelectTreeOperation(Selector Target, string Path) : ScenarioOperation(ScenarioActions.SelectTree);
