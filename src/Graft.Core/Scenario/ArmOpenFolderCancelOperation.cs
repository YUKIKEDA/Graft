namespace Graft.Core.Scenario;

/// <summary>
/// Arms the next OpenFolder seam as cancel.
/// </summary>
public sealed record ArmOpenFolderCancelOperation()
    : ScenarioOperation(ScenarioActions.ArmOpenFolderCancel);
