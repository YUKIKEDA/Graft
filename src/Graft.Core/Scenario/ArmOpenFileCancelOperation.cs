namespace Graft.Core.Scenario;

/// <summary>
/// Arm the next Graft OpenFile seam as cancel.
/// </summary>
public sealed record ArmOpenFileCancelOperation() : ScenarioOperation(ScenarioActions.ArmOpenFileCancel);
