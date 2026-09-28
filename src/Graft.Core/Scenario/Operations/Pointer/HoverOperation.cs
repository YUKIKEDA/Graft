using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: hover over an element (SendInput).
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record HoverOperation(Selector Target) : ScenarioOperation(ScenarioActions.Hover);
