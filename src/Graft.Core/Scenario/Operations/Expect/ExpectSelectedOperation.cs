using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>selected</c> state.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Selected">Expected selection state.</param>
public sealed record ExpectSelectedOperation(Selector Target, bool Selected) : ScenarioOperation(ScenarioActions.ExpectSelected);
