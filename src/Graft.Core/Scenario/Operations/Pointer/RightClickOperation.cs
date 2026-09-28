using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: right-click an element.
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record RightClickOperation(Selector Target) : ScenarioOperation(ScenarioActions.RightClick);
