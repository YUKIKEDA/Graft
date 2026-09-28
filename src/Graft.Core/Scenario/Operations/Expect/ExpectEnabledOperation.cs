using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>enabled</c> state.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Enabled">Expected enabled state.</param>
public sealed record ExpectEnabledOperation(Selector Target, bool Enabled) : ScenarioOperation(ScenarioActions.ExpectEnabled);
