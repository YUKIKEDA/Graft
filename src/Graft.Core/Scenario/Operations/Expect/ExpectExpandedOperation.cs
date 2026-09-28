using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>expanded</c> state.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Expanded">Expected expand state.</param>
public sealed record ExpectExpandedOperation(Selector Target, bool Expanded) : ScenarioOperation(ScenarioActions.ExpectExpanded);
