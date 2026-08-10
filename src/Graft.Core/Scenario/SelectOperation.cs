namespace Graft.Core.Scenario;

/// <summary>
/// Select a single list/combo item by index.
/// </summary>
/// <param name="AutomationId">List or combo automation id.</param>
/// <param name="Index">Zero-based item index.</param>
public sealed record SelectOperation(string AutomationId, int Index)
    : ScenarioOperation(ScenarioActions.Select);
