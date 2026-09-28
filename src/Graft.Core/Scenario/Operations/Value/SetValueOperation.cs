using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// setValue on an element by automation id.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Value">Replacement text.</param>
public sealed record SetValueOperation(Selector Target, string Value) : ScenarioOperation(ScenarioActions.SetValue);
