using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>focused</c> state to be true.
/// </summary>
/// <param name="Target">Target automation id.</param>
public sealed record ExpectFocusedOperation(Selector Target) : ScenarioOperation(ScenarioActions.ExpectFocused);
