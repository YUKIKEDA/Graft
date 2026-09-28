using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expand an element by automation id.
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record ExpandOperation(Selector Target) : ScenarioOperation(ScenarioActions.Expand);
