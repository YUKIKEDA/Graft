namespace Graft.Core.Scenario;

/// <summary>
/// Arm the next Graft OpenFile seam to return <see cref="Path"/> (OK).
/// </summary>
/// <param name="Path">File path to return.</param>
public sealed record ArmOpenFileOperation(string Path) : ScenarioOperation(ScenarioActions.ArmOpenFile);
