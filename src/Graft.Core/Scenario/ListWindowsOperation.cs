namespace Graft.Core.Scenario;

/// <summary>
/// Lists open windows (side-effect free aside from agent listWindows RPC).
/// </summary>
public sealed record ListWindowsOperation() : ScenarioOperation(ScenarioActions.ListWindows);
