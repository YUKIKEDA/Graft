using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Scenario step: double-click an element (SendInput).
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record DoubleClickOperation(Selector Target) : ScenarioOperation(ScenarioActions.DoubleClick);
