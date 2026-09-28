using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Collapse an element by automation id.
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record CollapseOperation(Selector Target) : ScenarioOperation(ScenarioActions.Collapse);
