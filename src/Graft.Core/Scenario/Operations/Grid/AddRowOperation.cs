using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: add a DataGrid row.
/// </summary>
/// <param name="Target">DataGrid automation id.</param>
public sealed record AddRowOperation(Selector Target) : ScenarioOperation(ScenarioActions.AddRow);
