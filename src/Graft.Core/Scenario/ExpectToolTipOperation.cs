namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's open ToolTip display text.
/// </summary>
/// <param name="AutomationId">Target automation id.</param>
/// <param name="ToolTip">Expected ToolTip text.</param>
public sealed record ExpectToolTipOperation(string AutomationId, string ToolTip)
    : ScenarioOperation(ScenarioActions.ExpectToolTip);
