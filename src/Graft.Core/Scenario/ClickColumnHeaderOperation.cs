namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: click a DataGrid column header (sort UI).
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
/// <param name="ColumnKey">Column Header string.</param>
public sealed record ClickColumnHeaderOperation(string AutomationId, string ColumnKey)
    : ScenarioOperation(ScenarioActions.ClickColumnHeader);
