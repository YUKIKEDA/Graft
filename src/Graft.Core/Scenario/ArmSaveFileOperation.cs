namespace Graft.Core.Scenario;

/// <summary>
/// Arms the next SaveFile seam with a path (OK).
/// </summary>
/// <param name="Path">File path to return.</param>
public sealed record ArmSaveFileOperation(string Path) : ScenarioOperation(ScenarioActions.ArmSaveFile);
