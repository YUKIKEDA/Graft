using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's open ToolTip display text.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="ToolTip">Expected ToolTip text.</param>
public sealed record ExpectToolTipOperation(Selector Target, string ToolTip) : ScenarioOperation(ScenarioActions.ExpectToolTip);
