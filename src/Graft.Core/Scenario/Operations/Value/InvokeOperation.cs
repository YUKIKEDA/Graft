using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Invoke an element by automation id.
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record InvokeOperation(Selector Target) : ScenarioOperation(ScenarioActions.Invoke);
