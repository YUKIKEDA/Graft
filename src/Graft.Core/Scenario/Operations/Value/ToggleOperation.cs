using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Toggle an element by automation id.
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record ToggleOperation(Selector Target) : ScenarioOperation(ScenarioActions.Toggle);
