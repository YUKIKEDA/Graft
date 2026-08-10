namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: add a DataGrid row.
/// </summary>
/// <param name="AutomationId">DataGrid automation id.</param>
public sealed record AddRowOperation(string AutomationId)
    : ScenarioOperation(ScenarioActions.AddRow);
