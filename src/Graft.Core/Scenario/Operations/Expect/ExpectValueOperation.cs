using Graft.Core.Selectors;

namespace Graft.Core.Scenario;

/// <summary>
/// Expect an element's tree <c>value</c>.
/// </summary>
/// <param name="Target">Target automation id.</param>
/// <param name="Value">Expected tree value.</param>
public sealed record ExpectValueOperation(Selector Target, string Value) : ScenarioOperation(ScenarioActions.ExpectValue);
