namespace Graft.Core.Scenario;

/// <summary>
/// Arms the next OpenFolder seam with a folder path (OK).
/// </summary>
/// <param name="Path">Folder path to return.</param>
public sealed record ArmOpenFolderOperation(string Path)
    : ScenarioOperation(ScenarioActions.ArmOpenFolder);
