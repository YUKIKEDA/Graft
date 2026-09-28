using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>visible</c> state.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Visible">Expected visible state.</param>
public sealed record ExpectVisibleOperation(Selector Target, bool Visible) : ScenarioOperation(ScenarioActions.ExpectVisible);
