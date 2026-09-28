using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Wait until an element is not found or not visible.
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record ExpectGoneOperation(Selector Target) : ScenarioOperation(ScenarioActions.ExpectGone);
