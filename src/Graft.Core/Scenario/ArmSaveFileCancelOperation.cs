namespace Graft.Core.Scenario;

/// <summary>
/// Arms the next SaveFile seam as cancel.
/// </summary>
public sealed record ArmSaveFileCancelOperation()
    : ScenarioOperation(ScenarioActions.ArmSaveFileCancel);
