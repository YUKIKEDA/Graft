namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: delete selected DataGrid rows.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
public sealed record DeleteSelectedRowsOperation(string AutomationId)
    : ScenarioOperation(ScenarioActions.DeleteSelectedRows);
