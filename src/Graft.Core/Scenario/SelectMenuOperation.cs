namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: select a menu path under a Menu / ContextMenu root.
/// </summary>
/// <param name="AutomationId">Menu or open ContextMenu automation id.</param>
/// <param name="Path">Slash-separated AutomationId segments (root not included).</param>
public sealed record SelectMenuOperation(string AutomationId, string Path)
    : ScenarioOperation(ScenarioActions.SelectMenu);
