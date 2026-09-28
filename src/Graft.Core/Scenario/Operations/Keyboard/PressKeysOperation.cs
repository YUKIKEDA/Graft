using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: press one keyboard chord on an element.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Keys">Chord DSL (e.g. <c>Control+A</c>).</param>
public sealed record PressKeysOperation(Selector Target, string Keys) : ScenarioOperation(ScenarioActions.PressKeys);
