using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: delete selected DataGrid rows.
/// </summary>
/// <param name="Target">DataGrid automation id.</param>
public sealed record DeleteSelectedRowsOperation(Selector Target) : ScenarioOperation(ScenarioActions.DeleteSelectedRows);
