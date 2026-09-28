using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Wait until an element is present in the visual tree.
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record WaitForOperation(Selector Target) : ScenarioOperation(ScenarioActions.WaitFor);
