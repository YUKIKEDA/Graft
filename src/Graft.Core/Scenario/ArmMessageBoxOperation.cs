namespace Graft.Core.Scenario;

/// <summary>
/// Arms the next MessageBox.Show with a MessageBoxResult name.
/// </summary>
/// <param name="Result">Result name: None, OK, Cancel, Yes, or No.</param>
public sealed record ArmMessageBoxOperation(string Result)
    : ScenarioOperation(ScenarioActions.ArmMessageBox);
